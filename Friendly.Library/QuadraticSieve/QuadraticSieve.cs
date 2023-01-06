using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using Friendly.Library;
using Friendly.Library.Utility;

//====================================================================
// References
//====================================================================
//
// A. Kefa Rabah , 2006. Review of Methods for Integer Factorization
//    Applied to Cryptography. Journal of Applied Sciences, 6: 458-481.
//    https://scialert.net/fulltext/?doi=jas.2006.458.481
//
// B. Robert D. Silverman, The Multiple Polynomial Quadratic Sieve,
//    Mathematics of Computation, Volume 48, Number 177, January 1987,
//    pages 329-339.
//
// C. A. K. Lenstra & M. S. Manasse, Factoring With Two Large Primes,
//    Mathematics of Computation, Volume 63, Number 208, October 1994,
//    pages 785-798.
//
// D. Paul Leyland et al, MPQS With Three Large Primes,
//    Algorithmic Number Theory. ANTS 2002. Lecture Notes in Computer
//    Science, vol 2369. Springer, Berlin, Heidelberg.
//

namespace Friendly.Library.QuadraticSieve
{
   public class QuadraticSieve : INotifyProgress, ISerialize
   {
      private readonly IParameters _parameters;

      /// <summary>
      /// The original number being factored.
      /// </summary>
      private readonly BigInteger _nOrig;

      /// <summary>
      /// A small prime which is multiplied by the number being factored to
      /// obtain a factor base which is richer in small primes.
      /// </summary>
      private long _multiplier;

      /// <summary>
      /// _nOrig *_multiplier
      /// </summary>
      private BigInteger _n;

      /// <summary>
      /// The Sieve Interval.
      /// </summary>
      private int _M;

      /// <summary>
      /// Ceiling(sqrt(_n))
      /// </summary>
      private BigInteger _rootN;

      private FactorBase _factorBase;

      private IRelations _relations;

      private IMatrix _matrix;
      private IMatrixFactory _matrixFactory = new MatrixFactory();

      private MultiPolynomial _multipolynomial;
      private IEnumerator<Polynomial> _polynomials;
      private int _totalPolynomials;

      private TimeSpan _priorFactoringTime;
      private DateTime _startFactoring = DateTime.MaxValue;

      public event EventHandler<NotifyProgressEventArgs> Progress;

      /// <summary>
      /// The number of parallel threads to use for the Sieve.
      /// </summary>
      private int _degreeOfParallelism = 0;

      /// <summary>
      /// Set to true to instruct the Sieve tasks to pause while state is saved.
      /// </summary>
      private volatile bool _pauseSieveToSave = false;

      /// <summary>
      /// A Barrier for the Sieving Threads to use to signal that they have
      /// paused for saving state.
      /// </summary>
      private Barrier _saveStartBarrier = new Barrier(0);

      /// <summary>
      /// A Barrier for the Sieving Threads to wait on until saving is finished.
      /// </summary>
      private Barrier _saveDoneBarrier = new Barrier(0);

      private SerializationReason _saveReason;

      private const string QuadraticSieveNodeName = "quadraticsieve";
      public const string NumberNodeName = "n";
      public const string MultiplierNodeName = "multiplier";
      public const string SieveIntervalNodeName = "sieveinterval";
      public const string FactorBaseSizeNodeName = "factorbasesize";
      public const string StatisticsNodeName = "statistics";
      public const string StatisticNodeName = "statistic";
      public const string RelationsNodeName = "relations";
      private const string PolynomialsNodeName = "multipolynomial";

      /// <summary>
      /// Initializes an instance of the Quadratic Sieve algorithm.
      /// </summary>
      /// <param name="parameters">An IParameters instance to supply the
      /// algorithm's parameters.</param>
      /// <param name="n">The number to be factored by this Quadratic Sieve.</param>
      public QuadraticSieve(IParameters parameters, BigInteger n)
      {
         _parameters = parameters;
         _nOrig = n;
         _multiplier = 1;
         _n = n;
         _rootN = 1 + BigIntegerCalculator.SquareRoot(_n);  // assumes _n is not square.

         _factorBase = null;
         _relations = null;
         _matrix = null;

         _polynomials = null;
         _totalPolynomials = 0;
      }

      /// <summary>
      /// Initializes an instance of the Quadratic Sieve algorithm.  This
      /// instance is set up to restart an interrupted factorization.
      /// </summary>
      /// <param name="parameters">An IParameters instance to supply the
      /// algorithm's parameters.</param>
      /// <param name="filename">The path to the file containing the state
      /// information.</param>
      public QuadraticSieve(IParameters parameters, string filename)
      {
         _parameters = parameters;

         XmlDocument doc = new XmlDocument();
         using (Stream strm = new FileStream(filename, FileMode.Open, FileAccess.Read))
         using (GZipStream gz = new GZipStream(strm, CompressionMode.Decompress))
         {
            using (Stream xsd = typeof(QuadraticSieve).Assembly.GetManifestResourceStream("Friendly.Library.Assets.QuadraticSieve.SaveQuadraticSieve.xsd")!)
               doc.Schemas.Add(XmlSchema.Read(xsd, null)!);

            doc.Load(gz);
         }

         XmlNode topNode = doc.FirstChild.NextSibling;
         SerializeHelper.ValidateNode(topNode, QuadraticSieveNodeName);

         XmlNode nNode = topNode.FirstChild;
         SerializeHelper.ValidateNode(nNode, NumberNodeName);
         _nOrig = SerializeHelper.ParseBigIntegerNode(nNode);

         XmlNode multiplierNode = nNode.NextSibling;
         SerializeHelper.ValidateNode(multiplierNode, MultiplierNodeName);
         _multiplier = SerializeHelper.ParseLongNode(multiplierNode);
         _n = _multiplier * _nOrig;
         _rootN = 1 + BigIntegerCalculator.SquareRoot(_n);

         XmlNode sieveIntervalNode = multiplierNode.NextSibling;
         SerializeHelper.ValidateNode(sieveIntervalNode, SieveIntervalNodeName);
         _M = SerializeHelper.ParseIntNode(sieveIntervalNode);

         XmlNode factorBaseSizeNode = sieveIntervalNode.NextSibling;
         SerializeHelper.ValidateNode(factorBaseSizeNode, FactorBaseSizeNodeName);
         int factorBaseSize = SerializeHelper.ParseIntNode(factorBaseSizeNode);
         _factorBase = FactorBaseCandidate.GetFactorBase((int)_multiplier, _nOrig, factorBaseSize);

         // Restore stats that we need to round trip
         List<Statistic> statistics = new();
         XmlNode statisticsNode = factorBaseSizeNode.NextSibling;
         SerializeHelper.ValidateNode(statisticsNode, StatisticsNodeName);
         XmlNode statNode = statisticsNode.FirstChild;
         while (statNode is not null)
         {
            statistics.Add(new Statistic(statNode));
            statNode = statNode.NextSibling;
         }
         _priorFactoringTime = (TimeSpan)(statistics.Where(s => s.Name == StatisticNames.FactoringTime).First().Value);
         _totalPolynomials = (int)(statistics.Where(s => s.Name == StatisticNames.TotalPolynomials).First().Value);

         XmlNode relationsNode = statisticsNode.NextSibling;
         SerializeHelper.ValidateNode(relationsNode, RelationsNodeName);
         _relations = (new RelationsFactory()).GetRelations(factorBaseSize,
            _factorBase[_factorBase.Count - 1].Prime, relationsNode);

         XmlNode polynomialsNode = relationsNode.NextSibling;
         SerializeHelper.ValidateNode(polynomialsNode, PolynomialsNodeName);
         _multipolynomial = new MultiPolynomial(_n, _rootN,
            _factorBase[_factorBase.Count - 1].Prime, _M, polynomialsNode);
         _polynomials = _multipolynomial.GetEnumerator();
      }

      /// <inheritdoc />
      public void BeginSerialize()
      {
         _pauseSieveToSave = true;

         // Wait for all Sieve Threads to pause.
         _saveStartBarrier.SignalAndWait();

         _relations.BeginSerialize();
         _multipolynomial.BeginSerialize();
      }

      /// <inheritdoc />
      public XmlNode Serialize(XmlDocument doc, string name)
      {
         XmlNode rv = doc.CreateElement(name);

         SerializeHelper.AddBigIntegerNode(doc, rv, NumberNodeName, _nOrig);
         SerializeHelper.AddLongNode(doc, rv, MultiplierNodeName, _multiplier);
         SerializeHelper.AddIntNode(doc, rv, SieveIntervalNodeName, _M);
         SerializeHelper.AddIntNode(doc, rv, FactorBaseSizeNodeName, _factorBase.Count);

         XmlNode statisticsNode = doc.CreateElement(StatisticsNodeName);
         rv.AppendChild(statisticsNode);
         foreach (Statistic j in GetStats())
            statisticsNode.AppendChild(j.Serialize(doc, StatisticNodeName));

         rv.AppendChild(_relations.Serialize(doc, RelationsNodeName));
         rv.AppendChild(_multipolynomial.Serialize(doc, PolynomialsNodeName));

         return rv;
      }

      /// <inheritdoc />
      public void FinishSerialize(SerializationReason reason)
      {
         _pauseSieveToSave = false;

         _multipolynomial.FinishSerialize(reason);
         _relations.FinishSerialize(reason);

         // Resume the Sieve Threads
         // We are the very last thread to call this, so there will be no
         // wait at all.
         _saveReason = reason;
         _saveDoneBarrier.SignalAndWait();
      }

      public void SaveState(SerializationReason reason, string filename)
      {
         XmlDocument doc = new XmlDocument();
         doc.AppendChild(doc.CreateXmlDeclaration("1.0", "UTF-8", null));
         Assembly assy = this.GetType().Assembly;
         using (Stream xsd = assy.GetManifestResourceStream("Friendly.Library.Assets.QuadraticSieve.SaveQuadraticSieve.xsd")!)
            doc.Schemas.Add(XmlSchema.Read(xsd, null)!);

         BeginSerialize();
         doc.AppendChild(Serialize(doc, QuadraticSieveNodeName));
         doc.Validate(null);

         using (Stream fs = new FileStream(filename, FileMode.CreateNew, FileAccess.Write))
         using (GZipStream gz = new GZipStream(fs, CompressionLevel.SmallestSize))
         {
            doc.Save(gz);
            gz.Flush();
            fs.Flush();
         }

         FinishSerialize(reason);
      }

      protected void OnNotifyProgress(string message)
      {
         TimeSpan factorTime = _priorFactoringTime + (DateTime.Now - _startFactoring);
         Progress?.Invoke(this, new NotifyProgressEventArgs(message, factorTime));
      }

      public int TotalPolynomials { get => _totalPolynomials; }

      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      public Statistic[] GetStats()
      {
         List<Statistic> rv = new();

         rv.AddRange(_relations.GetStats());

         Statistic timeStat = new Statistic(StatisticNames.FactoringTime,
            _priorFactoringTime + (DateTime.Now - _startFactoring));
         rv.Add(timeStat);

         rv.Add(new Statistic(StatisticNames.TotalPolynomials, _totalPolynomials));

         return rv.ToArray();
      }

      /// <summary>
      /// Factors the given number into two factors.
      /// </summary>
      /// <param name="n">The number to factor.</param>
      /// <returns>A tuple containing two factors.</returns>
      /// <remarks>
      /// <para>
      /// Pre-conditions:
      /// <list type="number">
      /// <item>Small prime factors have already been factored out.</item>
      /// <item>The given number, n, is not a power.</item>
      /// <item>n is not a prime number.</item>
      /// </list>
      /// </para>
      /// </remarks>
      public (BigInteger, BigInteger) Factor()
      {
         _startFactoring = DateTime.Now;

         if (_factorBase is null)
         {
            FindFactorBase();
            OnNotifyProgress($"The Factor Base contains {_factorBase.Count} primes.  Maximum prime: {_factorBase[_factorBase.Count - 1]}");

            int numDigits = BigIntegerCalculator.GetNumberOfDigits(_nOrig);
            int pmax = _factorBase[_factorBase.Count - 1].Prime;
            _relations = _parameters.GetRelationsFactory().GetRelations(numDigits, _factorBase.Count,
               pmax, ((long)pmax) * pmax);

            _M = _parameters.FindSieveInterval(_nOrig);
            _multipolynomial = new MultiPolynomial(_n, _rootN, _factorBase.MaxPrime, _M);
            _polynomials = _multipolynomial.GetEnumerator();
         }
         else
         {
            OnNotifyProgress($"Restarting Factorization with {_relations.Count} relations.");
         }

         int fbSize = _factorBase.Count;
         FindBSmooth(fbSize + 1);

         int retryCount = 0;
         int retryLimit = 100;
         int nullVectorsChecked = 0;
         while (retryCount < retryLimit)
         {
            OnNotifyProgress($"Have {_relations.Count} relations; building Matrix");
            _matrix = _relations.GetMatrix(_matrixFactory);
            OnNotifyProgress($"The Matrix has {_matrix.Columns} columns.");
            _matrix.Reduce();
            List<BigBitArray> nullVectors = _matrix.FindNullVectors();
            OnNotifyProgress($"Found {nullVectors.Count} Null Vectors");

            BigInteger x, y;
            foreach (BigBitArray nullVector in nullVectors)
            {
               nullVectorsChecked++;
               x = BigInteger.One;
               y = BigInteger.One;
               for (int j = 0, jul = _matrix.Columns; j < jul; j++)
               {
                  if (nullVector[j])
                  {
                     Relation relation = _relations[j];
                     x *= relation.X;
                     y *= relation.QOfX;
                  }
               }
               BigInteger t = BigIntegerCalculator.SquareRoot(y);
               Assertions.True(t * t == y);  // y was constructed to be a square.
               y = t;

               x %= _n;
               if (x < 0)
                  x += _n;
               y %= _n;

               // Is x = +/-y mod n?
               if (x == y || x + y == _n)
                  continue;

               BigInteger xmy = x - y;
               if (xmy < 0)
                  xmy += _n;

               BigInteger f1 = BigIntegerCalculator.GCD(_n, xmy);
               if (f1 != 1 && f1 != _n && f1 != _nOrig && f1 != _multiplier)
               {
                  BigInteger q = BigInteger.DivRem(f1, _multiplier, out BigInteger remainder);
                  if (remainder == 0)
                     f1 = q;
                  OnNotifyProgress($"Factored after checking {nullVectorsChecked} Null Vectors.");
                  return (f1, _nOrig / f1);
               }
            }

            retryCount++;
            OnNotifyProgress($"Retry number: {retryCount}");
            PrepareToResieve();
            FindBSmooth(fbSize + 1);
         }

         // We've gone back and retried 100 times and run out of squares each
         // time. This is cause for suspicion.
         throw new ApplicationException($"Ran out of squares while factoring {_n:N0}\nFactor Base Count: {_factorBase.Count}");
      }

      /// <summary>
      /// A utility method for timing how fast the current parameter set is at
      /// finding Relations.
      /// </summary>
      /// <param name="numRelations">The number of relations to stop after.</param>
      /// <returns>The number of seconds to find the specified number of relations.
      /// Time spent finding the factor base, etc is not included.</returns>
      public double ParameterTest(int numRelations)
      {
         _startFactoring = DateTime.Now;

         FindFactorBase();
         OnNotifyProgress($"The Factor Base contains {_factorBase.Count} primes.  Maximum prime: {_factorBase[_factorBase.Count - 1]}");

         int numDigits = BigIntegerCalculator.GetNumberOfDigits(_nOrig);
         int pmax = _factorBase[_factorBase.Count - 1].Prime;
         _relations = _parameters.GetRelationsFactory().GetRelations(numDigits, _factorBase.Count,
            pmax, ((long)pmax) * pmax);

         _M = _parameters.FindSieveInterval(_nOrig);
         _multipolynomial = new MultiPolynomial(_n, _rootN, _factorBase.MaxPrime, _M);
         _polynomials = _multipolynomial.GetEnumerator();

         Stopwatch sw = new();
         sw.Start();
         FindBSmooth(numRelations);
         sw.Stop();

         return sw.Elapsed.TotalSeconds;
      }

      /// <summary>
      /// Determines an appropriate factor base for factoring the given number
      /// </summary>
      private void FindFactorBase()
      {
         _factorBase = FactorBaseCandidate.GetFactorBase(_parameters, _nOrig);
         _multiplier = _factorBase.Multiplier;
         _n = _multiplier * _nOrig;
         _rootN = 1 + BigIntegerCalculator.SquareRoot(_n);  // assumes _n is not square.
      }

      /// <summary>
      /// Sieves for a list of B-Smooth numbers
      /// </summary>
      /// <remarks>
      /// <para>
      /// Each column of the Matrix contains the Exponent Vector for the B-Smooth number at
      /// the same index.
      /// </para>
      /// </remarks>
      private void FindBSmooth(int numRelationsNeeded)
      {
         int sieveSize = 2 * _M + 1;
         ushort[] sieve = new ushort[sieveSize];

         double T = _parameters.FindLargePrimeTolerance(_nOrig);
         long pmax = _factorBase[_factorBase.Count - 1].Prime;
         double pmaxt = Math.Pow(pmax, T);

         int smallPrimeLimit = _parameters.FindSmallPrimeLimit(_nOrig);
         float smallPrimeLog = 0;
         int firstPrimeIndex = 2;
         while (_factorBase[firstPrimeIndex].Prime <= smallPrimeLimit)
         {
            smallPrimeLog += _factorBase[firstPrimeIndex].Log;
            firstPrimeIndex++;
         }

         // The Relations class is not thread-safe.
         if (_relations is Relations)
            _degreeOfParallelism = 1;
         else
            _degreeOfParallelism = Environment.ProcessorCount / 2;

         AdjustBarrierCounts();

         ParallelOptions options = new ParallelOptions();
         options.MaxDegreeOfParallelism = _degreeOfParallelism;
         options.CancellationToken = (new CancellationTokenSource()).Token;

         Parallel.ForEach<Polynomial>(Polynomials(), options,
            (poly, state) => DoOneSieve(state, poly, numRelationsNeeded, pmaxt,
            firstPrimeIndex, smallPrimeLog));

         // Abort is detected by having left the Parallel.ForEach loop prior
         // to finding the required number of Relation objects.
         if (_relations.Count < numRelationsNeeded)
            throw new AbortException();
      }

      private void AdjustBarrierCounts()
      {
         if (_saveStartBarrier.ParticipantCount > 1 + _degreeOfParallelism)
            _saveStartBarrier.RemoveParticipants(1 + _degreeOfParallelism - _saveStartBarrier.ParticipantCount);
         else if (_saveStartBarrier.ParticipantCount < 1 + _degreeOfParallelism)
            _saveStartBarrier.AddParticipants(1 + _degreeOfParallelism - _saveStartBarrier.ParticipantCount);

         if (_saveDoneBarrier.ParticipantCount > 1 + _degreeOfParallelism)
            _saveDoneBarrier.RemoveParticipants(1 + _degreeOfParallelism - _saveDoneBarrier.ParticipantCount);
         else if (_saveDoneBarrier.ParticipantCount < 1 + _degreeOfParallelism)
            _saveDoneBarrier.AddParticipants(1 + _degreeOfParallelism - _saveDoneBarrier.ParticipantCount);
      }

      private IEnumerable<Polynomial> Polynomials()
      {
         while (_polynomials.MoveNext())
         {
            _totalPolynomials++;
            yield return _polynomials.Current;
         }
         yield break;
      }

      private void DoOneSieve(ParallelLoopState state, Polynomial poly,
         int numRelationsNeeded, double pmaxt, int firstPrimeIndex, float smallPrimeLog)
      {
         int sieveSize = 2 * _M + 1;
         ushort[] sieve = new ushort[sieveSize];

         // The Sieve Threshold is per Ref. B, Section 4 (iii),
         // adjusted for the exclusion of small primes from sieving.
         ushort sieveThreshold = (ushort)Math.Round(Math.Log(_M * Math.Sqrt((double)_n / 2) / pmaxt) - smallPrimeLog);

         // for all primes in the factor base (other than -1 and 2) add Add log(p) to the sieve
         for (int j = firstPrimeIndex, jul = _factorBase.Count; j < jul; j++)
         {
            FactorBasePrime prime = _factorBase[j];
            ushort log = (ushort)prime.Log;

            // Find the roots of Q(x) mod p.
            int curPrime = prime.Prime;
            int rootnModP = prime.RootNModP;
            BigInteger inv2a = BigIntegerCalculator.FindInverse(2 * poly.A, curPrime);  // BUG 2 is not invertible
            int x1 = (int)((-poly.B + rootnModP) * inv2a % curPrime);
            int x2 = (int)((-poly.B - rootnModP) * inv2a % curPrime);

            // Translate to the first index of Q and exponentVectors where
            // the values will divide evenly
            int offset = (int)(_M % curPrime);
            int index1 = x1 + offset;
            if (index1 < 0) index1 += curPrime;
            if (index1 >= curPrime) index1 -= curPrime;
            int index2 = x2 + offset;
            if (index2 < 0) index2 += curPrime;
            if (index2 >= curPrime) index2 -= curPrime;

            // Add the Log of the Prime to each r +/- p location.
            AddLogs(sieve, index1, curPrime, log);
            AddLogs(sieve, index2, curPrime, log);
         }

         // Find all sieve locations which exceed the Sieve Threshold
         for (int x = -_M, idx = 0; x <= _M; x++, idx++)
         {
            if (sieve[idx] > sieveThreshold)
            {
               BigBitArray exponentVector = new BigBitArray(_factorBase.Count);
               BigInteger Q = poly.Evaluate(x);
               BigInteger origQ = Q;

               // Handle the -1 prime
               if (Q < 0)
               {
                  Q *= -1;
                  exponentVector.FlipBit(0);
               }

               // Handle the remaining primes
               for (int j = 1, jul = _factorBase.Count; j < jul; j++)
               {
                  int curPrime = _factorBase[j].Prime;
                  BigInteger q, r;
                  q = BigInteger.DivRem(Q, curPrime, out r);
                  while (r == 0)
                  {
                     Q = q;
                     exponentVector.FlipBit(j);
                     q = BigInteger.DivRem(Q, curPrime, out r);
                  }
               }

               _relations.TryAddRelation(origQ, poly.EvaluateLHS(x), exponentVector, Q);
            }
         }

         if (_relations.Count > numRelationsNeeded)
         {
            state.Stop();
            return;
         }

         if (_pauseSieveToSave)
         {
            _saveStartBarrier.SignalAndWait();
            _saveDoneBarrier.SignalAndWait();

            if (_saveReason == SerializationReason.Shutdown)
            {
               state.Stop();
               return;
            }
         }
      }

      private unsafe static void AddLogs(ushort[] sieve, int startIndex, int stride, ushort log)
      {
         fixed (ushort *pFixed = sieve)
         for (ushort* p = pFixed + startIndex, pEnd = pFixed + sieve.Length; p < pEnd; p += stride)
            *p += log;
      }

      /// <summary>
      /// Prepares for another round of Sieving
      /// </summary>
      /// <remarks>
      /// <para>
      /// Call this after the set of null vectors has failed to produce a
      /// factorization.  The set of free variables is discarded, and the
      /// Exponent Vector Matrix is recalculated.
      /// </para>
      /// </remarks>
      public void PrepareToResieve()
      {
         // TODO: need to be able to restart enumeration of Polynomials.
         throw new ApplicationException("No null vector yielded a factorization.");
         //// Remove the free columns which generated the non-useful null vectors.
         //List<int> freeColumns = _matrix.FindFreeColumns();
         //_matrix = null;
         //for (int j = freeColumns.Count - 1; j >= 0; j--)
         //   _relations.RemoveRelationAt(j);
      }
   }
}
