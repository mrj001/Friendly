using System;

namespace Friendly.Library
{
   /// <summary>
   /// 
   /// </summary>
   public class BigBitArray
   {
      private long _capacity;
      private ulong[] _bits;

      /// <summary>
      /// Constructs an instance of BigBitArray.
      /// </summary>
      /// <param name="capacity">Specifies the number of bits in the BigBitArray.</param>
      /// <remarks>
      /// <para>
      /// If capacity is not a multiple of 64, it is increased to the next multiple of 64.
      /// </para>
      /// <para>
      /// All bits are initially cleared.
      /// </para>
      /// </remarks>
      public BigBitArray(long capacity)
      {
         if (capacity <= 0)
            throw new ArgumentOutOfRangeException($"{nameof(capacity)} must be greater than 0.");

         long nlongs = (capacity + 63) / 64;
         _capacity = nlongs * 64;
         _bits = new ulong[nlongs];
      }

      /// <summary>
      /// Constructs a new BigBitArray that is a duplicate of the given one.
      /// </summary>
      /// <param name="other">The BigBitArray instance to duplicate.</param>
      public BigBitArray(BigBitArray other)
      {
         _capacity = other._capacity;
         _bits = new ulong[other._bits.Length];
         for (int j = 0; j < _bits.Length; j++)
            _bits[j] = other._bits[j];
      }

      /// <summary>
      /// Gets the number of bits in this BigBitArray.
      /// </summary>
      public long Capacity { get => _capacity; }

      /// <summary>
      /// Gets or sets the bit at the given index.
      /// </summary>
      /// <param name="index">The index of the bit to operate on.</param>
      /// <returns>True if the bit is set; false if the bit is cleared.</returns>
      public bool this[long index]
      {
         get
         {
#if DEBUG
            if (index < 0)
               throw new IndexOutOfRangeException();
#endif

            ulong bit = 1UL << (int)(index % 64);
            return (_bits[index / 64] & bit) != 0;
         }
         set
         {
            ulong bit = 1UL << (int)(index % 64);
            if (value)
               _bits[index / 64] |= bit;
            else
               _bits[index / 64] &= ~bit;
         }
      }

      /// <summary>
      /// Flips the bit at the specified Index.
      /// </summary>
      /// <param name="index">The bit to change the value of.</param>
      public void FlipBit(long index)
      {
         ulong bit = 1UL << (int)(index % 64);
         _bits[index / 64] ^= bit;
      }

      /// <summary>
      /// Expands the number of bits in this Bit Array.
      /// </summary>
      /// <param name="newCapacity">The minimum new Capacity.</param>
      /// <remarks>
      /// <para>
      /// If newCapacity is not a multiple of 64, it is increased to the next multiple of 64.
      /// </para>
      /// <para>
      /// All new bits are initially cleared.
      /// </para>
      /// </remarks>
      public void Expand(long newCapacity)
      {
         long oldNumLongs = _bits.Length;
         long nlongs = (newCapacity + 63) / 64;
         if (nlongs < oldNumLongs)
            return;

         ulong[] newBits = new ulong[nlongs];
         for (int j = 0; j < oldNumLongs; j++)
            newBits[j] = _bits[j];

         _capacity = nlongs * 64;
         _bits = newBits;
      }

      /// <summary>
      /// Updates this Big Bit Array with the Xor of this Big Bit Array with the other.
      /// </summary>
      /// <param name="other"></param>
      public void Xor(BigBitArray other)
      {
         long jul = Math.Min(this.Capacity, other.Capacity) / 64;
         for (long j = 0; j < jul; j++)
            _bits[j] ^= other._bits[j];
      }
   }
}

