using System.Numerics;

namespace Friendly.Library
{
   public interface IPrimeFactor
   {
      BigInteger Factor { get; }
      int Exponent { get; }
   }
}