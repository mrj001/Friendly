using System;

namespace Friendly.Library
{
   public class Fraction : IEquatable<Fraction>, IComparable<Fraction>
   {
      private readonly long _num;

      private readonly long _den;

      public static readonly Fraction Zero = new Fraction(0, 1);
      public static readonly Fraction One  = new Fraction(1, 1);

      public Fraction(long num, long den)
      {
         if (den == 0)
            throw new DivideByZeroException();

         // Determine the sign.
         // For negative fractions, we will store the numerator as negative.
         int negate = 0;
         if (num < 0)
         {
            negate ++;
            num *= -1;
         }
         if (den < 0)
         {
            negate ++;
            den *= -1;
         }
         negate = (negate == 1 ? -1 : 1);

         // Record fraction with the denominator reduced.
         long f = gcd(den, num);
         if (f != 1)
         {
            _num = negate * num / f;
            _den = den / f;
         }
         else
         {
            _num = negate * num;
            _den = den;
         }
      }

      private static long gcd(long a, long b)
      {
         if (b == 0)
            return a;

         return gcd(b, a % b);
      }

      private static long LeastCommonMultiple(long a, long b)
      {
         a /= gcd((a > b ? a : b), (a <= b ? a : b));
         return a * b;
      }

      public long Numerator { get => _num; }

      public long Denominator { get => _den; }

      public Fraction GetInverse()
      {
         if (_num == 0)
            throw new DivideByZeroException();
         return new Fraction(_den, _num);
      }

#region Object overrides
      public override bool Equals(object obj)
      {
         return Equals(obj as Fraction);
      }

      public override int GetHashCode()
      {
         return _num.GetHashCode() * 3 + _den.GetHashCode();
      }

      public override string ToString()
      {
         return $"{_num}/{_den}";
      }
 
#endregion

#region IEquatable<Fraction> & related
      public bool Equals(Fraction other)
      {
         if (object.ReferenceEquals(other, null))
            return false;

         return _num == other.Numerator && _den == other.Denominator;
      }

      public static bool operator==(Fraction l, Fraction r)
      {
         if (!object.ReferenceEquals(l, null))
            return l.Equals(r);
         else if (!object.ReferenceEquals(r, null))
            return false;
         else
            return true;
      }

      public static bool operator!=(Fraction l, Fraction r)
      {
         return !(l == r);
      }
#endregion

#region IComparable<Fraction> & related
      public int CompareTo(Fraction other)
      {
         // null must sort first.
         if (object.ReferenceEquals(other, null))
            return +1;

         return (_num * other.Denominator).CompareTo(other.Numerator * _den);
      }

      public static bool operator >(Fraction l, Fraction r)
      {
         if (!object.ReferenceEquals(l, null))
            return (l.CompareTo(r) > 0);

         // if r is not null, l is strictly less than r.
         // If r is null, l and r are equal.
         // In either case, l is NOT greater than r.
         return false;
      }

      public static bool operator >=(Fraction l, Fraction r)
      {
         if (!object.ReferenceEquals(l, null))
            return l.CompareTo(r) >= 0;

         // if r is not null, l is strictly less than r, then
         // l is NOT greater than or equal to r.
         if (!object.ReferenceEquals(r, null))
            return false;

         // Both are null.  Therefore, l is equal to r.
         return true;
      }

      public static bool operator <(Fraction l, Fraction r)
      {
         return !(l >= r);
      }

      public static bool operator <=(Fraction l, Fraction r)
      {
         return !(l > r);
      }
#endregion

#region Arithmetic
      private static Fraction Add(long an, long ad, long bn, long bd)
      {
         // Make sure the denominators are the same.
         if (ad != bd)
         {
            long lcm = LeastCommonMultiple(ad, bd);
            long am = lcm / ad;
            long bm = lcm / bd;
            ad *= am;
            an *= am;
            bd *= bm;
            bn *= bm;
         }

         return new Fraction(an + bn, ad);
      }

      public static Fraction operator+(Fraction a, Fraction b)
      {
         return Add(a.Numerator, a.Denominator, b.Numerator, b.Denominator);
      }

      public static Fraction operator-(Fraction a, Fraction b)
      {
         return Add(a.Numerator, a.Denominator, -b.Numerator, b.Denominator);
      }

      public static Fraction operator*(Fraction a, Fraction b)
      {
         // If possible, cancel any common factors between the 
         // numerator of a and the denominator of b.
         long an = a.Numerator;
         long bd = b.Denominator;
         long gcf = gcd((an > bd ? an : bd), (an <= bd ? an : bd));
         if (gcf != 1)
         {
            an /= gcf;
            bd /= gcf;
         }

         // If possible, cancel any common factors between the
         // numerator of b and the denominator of a.
         long bn = b.Numerator;
         long ad = a.Denominator;
         gcf = gcd((bn > ad ? bn : ad), (bn <= ad ? bn : ad));
         if (gcf != 1)
         {
            bn /= gcf;
            ad /= gcf;
         }

         return new Fraction(an * bn, ad * bd);
      }

      public static Fraction operator/(Fraction a, Fraction b)
      {
         return new Fraction(a.Numerator * b.Denominator, a.Denominator * b.Numerator);
      }

      public static Fraction operator-(Fraction x)
      {
         return new Fraction(-x.Numerator, x.Denominator);
      }
#endregion
   }
}
