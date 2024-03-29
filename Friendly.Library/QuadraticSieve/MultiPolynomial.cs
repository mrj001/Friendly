﻿#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Xml;
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

namespace Friendly.Library.QuadraticSieve
{
   public class MultiPolynomial : IEnumerable<Polynomial>, ISerialize
   {
      /// <summary>
      /// Set to true if we are starting the enumeration of polynomials from
      /// a saved point.
      /// </summary>
      private readonly bool _restart;
      private readonly BigInteger _currentD = 0;
      private readonly BigInteger _lowerD = long.MaxValue;
      private readonly BigInteger _higherD = long.MinValue;
      private readonly bool _nextDHigher = false;

      private readonly BigInteger _kn;
      private readonly BigInteger _rootkn;
      private readonly long _maxFactorBase;
      private readonly int _M;

      private Enumerator? _enumerator;

      private const string CurrentDNodeName = "currentd";
      private const string LowerDNodeName = "lowerd";
      private const string HigherDNodeName = "higherd";
      private const string NextDHigherNodeName = "nextdhigher";

      /// <summary>
      /// Constructs a new Enumerable object for the Multiple Polynomials.
      /// </summary>
      /// <param name="kn">The number being factored, pre-multiplied by the small constant.</param>
      /// <param name="rootkn">The ceiling of the square root of kn.</param>
      /// <param name="maxFactorBase">The largest prime in the Factor Base.</param>
      /// <param name="M">The Sieve Interval</param>
      public MultiPolynomial(BigInteger kn, BigInteger rootkn, long maxFactorBase, int M)
      {
         _restart = false;
         _kn = kn;
         _rootkn = rootkn;
         _maxFactorBase = maxFactorBase;
         _M = M;
      }

      /// <summary>
      /// Constructs a new Enumerable object for Multiple Polynomials where
      /// some of the polynomials have already been used.
      /// </summary>
      /// <param name="kn">The number being factored, pre-multiplied by the small constant.</param>
      /// <param name="rootkn">The ceiling of the square root of kn.</param>
      /// <param name="maxFactorBase">The largest prime in the Factor Base.</param>
      /// <param name="M">The Sieve Interval</param>
      /// <param name="rdr">An XMLReader positioned at the &lt;multipolynomial&gt; node.</param>
      public MultiPolynomial(BigInteger kn, BigInteger rootkn, long maxFactorBase,
         int M, XmlReader rdr)
      {
         _restart = true;
         _kn = kn;
         _rootkn = rootkn;
         _maxFactorBase = maxFactorBase;
         _M = M;

         rdr.ReadStartElement();

         rdr.ReadStartElement(CurrentDNodeName);
         _currentD = SerializeHelper.ParseBigIntegerNode(rdr);
         rdr.ReadEndElement();

         rdr.ReadStartElement(LowerDNodeName);
         _lowerD = SerializeHelper.ParseBigIntegerNode(rdr);
         rdr.ReadEndElement();

         rdr.ReadStartElement(HigherDNodeName);
         _higherD = SerializeHelper.ParseBigIntegerNode(rdr);
         rdr.ReadEndElement();

         rdr.ReadStartElement(NextDHigherNodeName);
         string innerText = rdr.ReadContentAsString();
         if (!bool.TryParse(innerText, out _nextDHigher))
            throw new ArgumentException($"Failed to parse '{innerText}' for <{NextDHigherNodeName}>.");
         rdr.ReadEndElement();

         rdr.ReadEndElement();
      }

      public void BeginSerialize()
      {
         _enumerator?.BeginSerialize();
      }

      public void Serialize(XmlWriter writer, string name)
      {
         if (_enumerator is not null)
            _enumerator.Serialize(writer, name);
         else
            (new Enumerator(_kn, _rootkn, _maxFactorBase, _M)).Serialize(writer, name);
      }

      public void FinishSerialize(SerializationReason reason)
      {
         _enumerator?.FinishSerialize(reason);
      }

      public IEnumerator<Polynomial> GetEnumerator()
      {
         if (_restart)
            _enumerator = new Enumerator(_kn, _rootkn, _maxFactorBase, _M, _currentD, _lowerD, _higherD, _nextDHigher);
         else
            _enumerator = new Enumerator(_kn, _rootkn, _maxFactorBase, _M);

         return _enumerator;
      }

      IEnumerator IEnumerable.GetEnumerator()
      {
         if (_restart)
            _enumerator = new Enumerator(_kn, _rootkn, _maxFactorBase, _M, _currentD, _lowerD, _higherD, _nextDHigher);
         else
            _enumerator = new Enumerator(_kn, _rootkn, _maxFactorBase, _M);

         return _enumerator;
      }

      private class Enumerator : IEnumerator<Polynomial>, ISerialize
      {
         private readonly BigInteger _kn;
         private readonly BigInteger _rootkn;
         private readonly long _maxFactorBase;

         private bool _restarted;

         /// <summary>
         /// The "ideal" choice of D in Ref. B, section 3.
         /// </summary>
         private readonly BigInteger _idealD;
         private BigInteger _currentD = 0;
         private BigInteger _lowerD = long.MaxValue;
         private BigInteger _higherD = long.MinValue;
         private bool _nextDHigher = false;

         /// <summary>
         /// 
         /// </summary>
         /// <param name="kn">The number being factored, premultiplied by a small constant.</param>
         /// <param name="rootkn">The ceiling of the square root of kn.</param>
         /// <param name="maxFactorBase">The largest prime in the Factor Base.</param>
         /// <param name="M">The Sieve Interval</param>
         public Enumerator(BigInteger kn, BigInteger rootkn, long maxFactorBase, int M)
         {
            _kn = kn;
            _rootkn = rootkn;
            _maxFactorBase = maxFactorBase;
            _restarted = false;

            _idealD = CalculateIdealD(_rootkn, M);

            Reset();
         }

         /// <summary>
         /// Constructs a new Enumerator that starts from a specified point
         /// after the usual beginning
         /// </summary>
         /// <param name="kn">The number being factored, premultiplied by a small constant.</param>
         /// <param name="rootkn">The ceiling of the square root of kn.</param>
         /// <param name="maxFactorBase">The largest prime in the Factor Base.</param>
         /// <param name="M">The Sieve Interval</param>
         /// <param name="currentD"></param>
         /// <param name="lowerD"></param>
         /// <param name="higherD"></param>
         /// <param name="nextDHigher"></param>
         public Enumerator(BigInteger kn, BigInteger rootkn, long maxFactorBase,
            int M, BigInteger currentD, BigInteger lowerD, BigInteger higherD, bool nextDHigher)
         {
            _kn = kn;
            _rootkn = rootkn;
            _maxFactorBase = maxFactorBase;
            _restarted = true;

            _idealD = CalculateIdealD(_rootkn, M);

            _currentD = currentD;
            _lowerD = lowerD;
            _higherD = higherD;
            _nextDHigher = nextDHigher;
         }

         private static BigInteger CalculateIdealD(BigInteger rootkn, int M)
         {
            BigInteger t = rootkn / (2 * M);
            BigInteger idealD = BigIntegerCalculator.SquareRoot(t);
            if ((idealD & 1) == 0)
               idealD--;

            return idealD;
         }

         /// <inheritdoc />
         public void BeginSerialize()
         {
            // Nothing to do here.
         }

         /// <inheritdoc />
         public void Serialize(XmlWriter writer, string name)
         {
            writer.WriteStartElement(name);

            writer.WriteElementString(CurrentDNodeName, _currentD.ToString());
            writer.WriteElementString(LowerDNodeName, _lowerD.ToString());
            writer.WriteElementString(HigherDNodeName, _higherD.ToString());
            writer.WriteElementString(NextDHigherNodeName, _nextDHigher ? "true" : "false");

            writer.WriteEndElement();
         }

         /// <inheritdoc />
         public void FinishSerialize(SerializationReason reason)
         {
            // nothing to do here.
         }


         public Polynomial Current => InternalCurrent();

         object IEnumerator.Current => InternalCurrent();

         private Polynomial InternalCurrent()
         {
            //// First option in Ref A
            //long d = _primes.Current;
            //long a = d * d;

            //// Solve for B in B**2 == kn mod a;
            //long b = LongCalculator.SquareRoot(_kn, d);
            //Assertions.True(b * b % d == _kn % d);

            //// Apply Hensel's Lemma to lift the root to mod a
            //long t = _kn - b * b;
            //Assertions.True(t % d == 0);
            //t /= d;
            //long inv2b = LongCalculator.FindInverse(2 * b, d);
            //t *= inv2b;
            //t %= d;
            //b += t * d;
            //if ((b & 1) == 0) b = a - b;    // TODO: why?
            //// Confirm the root was lifted correctly.
            //Assertions.True(b * b % a == _kn % a);

            //long c = (b * b - _kn);
            //Assertions.True(c % a == 0);
            //c /= a;



            //// Second option in Ref A
            //// Choose a to be a prime
            //long a = _primes.Current;

            //// Solve for b in b**2 == n mod a
            //long b = LongCalculator.SquareRoot(_kn, a);

            //Assertions.True((b * b - _kn) % a == 0);
            //long c = (b * b - _kn) / a;


            // Finding Coefficients per Ref B.
            BigInteger d = _currentD;
            BigInteger a = d * d;

            // Ref. B, Equations 7a & 7b.
            BigInteger h0 = BigInteger.ModPow(_kn, (d - 3) / 4, d);
            if ((h0 & 1) == 1)
               h0 += d;
            //long h1 = ((_kn % d) * h0) % d;
            BigInteger h1 = BigInteger.ModPow(_kn, (d + 1) / 4, d);
            Assertions.True((h0 * h1) % d == 1);

            // Ref B. Equation 8
            BigInteger h1squared = h1 * h1;
            Assertions.True(h1squared % d == _kn % d);

            // Ref B. Equation 9
            BigInteger num = _kn - h1squared;
            Assertions.True(num % d == 0);
            Assertions.True((h0 & 1) == 0);
            Assertions.True(((h0 / 2) * (2 * h1)) % d == 1);
            BigInteger h2 = ((h0 / 2) * (num / d)) % d;

            // Ref B. Equation 10
            BigInteger b = (h1 + h2 * d) % a;
            if ((b & 1) == 0) b = a - b;

            // Ref B. Equation 11
            //long bsquared = h1 * h1 + 2 * h1 * h2 * d + h2 * h2 * d * d;
            Assertions.True((h1 * h1 + 2 * h1 * h2 * d + h2 * h2 * d * d) % a == b * b % a);
            BigInteger bsquared = b * b;
            Assertions.True(bsquared % a == _kn % a);

            // Ref B. Equation 12
            BigInteger c = bsquared - _kn;
            Assertions.True((c % (4 * a)) == 0);
            c /= 4 * a;

            return new Polynomial(a, b, c, BigIntegerCalculator.FindInverse(2 * d, _kn));
         }

         public void Dispose()
         {
            // Empty
         }

         public bool MoveNext()
         {
            BigInteger d;

            if (_restarted)
            {
               _restarted = false;
               return true;
            }

            if (!_nextDHigher)
            {
               d = _lowerD;
               do
                  d -= 2;
               while (d > _maxFactorBase && !IsSuitablePrime(d));
               _lowerD = d;
               _nextDHigher = true;
               if (d > _maxFactorBase)
               {
                  _currentD = _lowerD;
                  return true;
               }
            }

            d = _higherD;
            do
               d += 2;
            while (!IsSuitablePrime(d));
            _higherD = d;
            _currentD = d;
            _nextDHigher = _lowerD > _maxFactorBase;

            return true;
         }

         private bool IsSuitablePrime(BigInteger p)
         {
            // A suitable prime must meet all of the following conditions:
            //   1. Be Prime.
            //   2. Be larger than the Factor Base
            //   3. Legendre(kN/D) == 1 (i.e. kN is a quadratic residue mod D)
            //   4. D == 3 mod 4
            return Primes.IsPrime(p) && p > _maxFactorBase &&
               1 == BigIntegerCalculator.JacobiSymbol(_kn, p) && (p & 3) == 3;
         }

         public void Reset()
         {
            _currentD = long.MinValue;
            _lowerD = _idealD + 2;
            _higherD = _idealD > _maxFactorBase ? _idealD : _maxFactorBase;
            _nextDHigher = _lowerD <= _maxFactorBase;
         }
      }
   }
}
