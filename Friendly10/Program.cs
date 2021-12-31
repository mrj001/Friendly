using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using Friendly.Library;

namespace Friendly10
{
   public class Program
   {
      private static bool _isStopCommanded = false;

      private static TimeSpan _lastStateSave;
      private static object _lockStateSave = new object();

      // The number of minutes between saves of the current state.
      private static readonly int _stateSaveMinutes = 15;

      static void Main(string[] args)
      {
         Stopwatch watch = Stopwatch.StartNew();
         string fmtTimeStamp = "d\\.hh\\:mm\\:ss\\.fff";

         Console.WriteLine("Sieving");
         Primes.Init(4_294_967_296);
         Console.WriteLine("{0}: Sieving completed", watch.Elapsed.ToString(fmtTimeStamp));

         // Handle Ctrl+C
         Console.CancelKeyPress += (s, e) => {
            _isStopCommanded = true;
            e.Cancel = true;
            Console.WriteLine("Stopping...");
         };

         long target = 10;
         Fraction targetAbundancyIndex = AbundancyIndex(target);

         int maxDegreeOfParallelism = 2;
         ParallelOptions options = new ParallelOptions();
         options.MaxDegreeOfParallelism = maxDegreeOfParallelism;
         options.CancellationToken = (new CancellationTokenSource()).Token;

         HashSet<long> curLoopIndices = new HashSet<long>(2 * maxDegreeOfParallelism);

         long start = GetStart(target, targetAbundancyIndex);
         Console.WriteLine($"Starting at {start:N0}; incrementing by {targetAbundancyIndex.Denominator}.");

         Parallel.ForEach<long>(
            Range(start, targetAbundancyIndex.Denominator, long.MaxValue),
            options, 
            // This function provides the loop body.
            (j, state) =>
            {
               lock(curLoopIndices)
               {
                  curLoopIndices.Add(j);
               }

               if (_isStopCommanded)
               {
                  state.Stop();
                  return;
               }

               // Is it time to save State?
               if (watch.Elapsed.Subtract(_lastStateSave).TotalMinutes > _stateSaveMinutes)
               {
                  lock(_lockStateSave)
                  {
                     // Did we get the lock because some other thread just finished saving?
                     // Or is this the thread that must save state?
                     if (watch.Elapsed.Subtract(_lastStateSave).TotalMinutes > _stateSaveMinutes)
                     {
                        SaveState(curLoopIndices);
                        _lastStateSave = watch.Elapsed;
                     }
                  }
               }

               if (j % 10_000 == 0)
                  Console.WriteLine("{0}: Checking {1:N0} (thread id: {2})", 
                     watch.Elapsed.ToString(fmtTimeStamp), j, Thread.CurrentThread.ManagedThreadId);

               Fraction abundancyIndex = null;
               try
               {
                  abundancyIndex = AbundancyIndex(j);
               }
               catch (OverflowException ex)
               {
                  Console.WriteLine("{0}: Integer overflow exception at j == {1:N0}", watch.Elapsed.ToString(fmtTimeStamp), j);
                  Console.WriteLine(ex.StackTrace);
                  state.Break();
                  return;
               }

               if (abundancyIndex == targetAbundancyIndex)
               {
                  Console.WriteLine("{0}: found it: {1:N0}", watch.Elapsed.ToString(fmtTimeStamp), j);
                  state.Break();
                  return;
               }

               lock(curLoopIndices)
               {
                  curLoopIndices.Remove(j);
               }
               return;
            });

         if (_isStopCommanded)
            SaveState(curLoopIndices);
      }

      private static void SaveState(HashSet<long> loopIndices)
      {
         long nextStart = long.MinValue;
         lock(loopIndices)
         {
            nextStart = loopIndices.Min();
         }

         StringBuilder sb = new StringBuilder();

         sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>");
         sb.AppendLine("<savepoint>");
         sb.AppendLine(nextStart.ToString("N0"));
         sb.AppendLine("</savepoint>");

         using (Stream strm = new FileStream(GetSaveFileName(), FileMode.Create))
         using (StreamWriter sw = new StreamWriter(strm))
            sw.Write(sb.ToString());
      }

      /// <summary>
      /// Gets the number at which we will start searching for the target Abundancy Index.
      /// </summary>
      /// <param name="target"></param>
      /// <param name="targetAbundancyIndex"></param>
      /// <returns></returns>
      /// <remarks>
      /// <para>
      /// In order for the Abundancy Index fraction to reduce to the same as
      /// the target Abundancy Index, the numbers being checked must be a
      /// multiple of the denominator.  
      /// </para>
      /// <para>
      /// If no saved state is found, the search will start at the next largest
      /// multiple of the target denominator.
      /// If saved state is found, the start value will be confirmed to be a 
      /// multiple of the target denominator.
      /// </para>
      /// </remarks>
      private static long GetStart(long target, Fraction targetAbundancyIndex)
      {
         // Check for saved state in the same folder as the executable.
         string saveFile = GetSaveFileName();
         if (File.Exists(saveFile))
         {
            XmlDocument doc = new XmlDocument();
            Assembly thisAssembly = typeof(Program).Assembly;
            using (Stream xsd = thisAssembly.GetManifestResourceStream("Friendly10.Assets.SavePoint.xsd"))
               doc.Schemas.Add(XmlSchema.Read(xsd, null));
            
            using (Stream xml = new FileStream(saveFile, FileMode.Open))
            using (XmlReader rdr = XmlReader.Create(xml))
            {
               doc.Load(rdr);
               doc.Validate(null);
            }

            XmlNode savePoint = doc.ChildNodes.OfType<XmlNode>().Where<XmlNode>(n => n.LocalName == "savepoint").First();
            long start = long.Parse(savePoint.InnerText, NumberStyles.Integer | NumberStyles.AllowThousands);
            if (start % targetAbundancyIndex.Denominator != 0)
               throw new ArgumentException($"Invalid value found in save file.  It must be a multiple of {targetAbundancyIndex.Denominator}.");

            return start;
         }
         else
         {
            return target + targetAbundancyIndex.Denominator - target % targetAbundancyIndex.Denominator;
         }
      }

      private static string GetSaveFileName()
      {
         return Path.Combine(
                  Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                  "SavePoint.xml");
      }

      private static IEnumerable<long> Range(long start, long increment, long limit)
      {
         long n = start;
         while (n < limit)
         {
            yield return n;
            n += increment;
         }
      }

      private static Fraction AbundancyIndex(long n)
      {
         PrimeFactorization pf = PrimeFactorization.Get(n);
         return new Fraction(pf.SumOfFactors, n);
      }
   }
}
