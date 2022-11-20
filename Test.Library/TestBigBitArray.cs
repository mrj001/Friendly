using System;
using System.Collections.Generic;
using Friendly.Library;
using Xunit;

namespace Test.Library
{
   public class TestBigBitArray
   {
      [Fact]
      public void ctor_throws()
      {
         Assert.Throws<ArgumentOutOfRangeException>(() => new BigBitArray(0));
         Assert.Throws<ArgumentOutOfRangeException>(() => new BigBitArray(-1));
      }

      public static TheoryData<long, long> ctor_TestData
      {
         get
         {
            var rv = new TheoryData<long, long>();
            rv.Add(128, 127);
            rv.Add(256, 256);

            return rv;
         }
      }

      [Theory]
      [MemberData(nameof(ctor_TestData))]
      public void ctor(long expectedCapacity, long inputCapacity)
      {
         BigBitArray tst = new BigBitArray(inputCapacity);

         Assert.Equal(expectedCapacity, tst.Capacity);
      }

      [Fact]
      public void indexer_throws()
      {
         int capacity = 1024;
         BigBitArray tst = new BigBitArray(capacity);

         Assert.Throws<IndexOutOfRangeException>(() => { bool x = tst[capacity]; });
#if DEBUG
         Assert.Throws<IndexOutOfRangeException>(() => { bool x = tst[-1]; });
#endif
      }

      public static TheoryData<long, long> indexer_TestData
      {
         get
         {
            var rv = new TheoryData<long, long>();
            rv.Add(1024, 0);
            rv.Add(2048, 2047);
            rv.Add(4192, 1234);

            return rv;
         }
      }


      [Theory]
      [MemberData(nameof(indexer_TestData))]
      public void indexer(long capacity, long index)
      {
         BigBitArray tst = new BigBitArray(capacity);

         tst[index] = true;

         Assert.True(tst[index]);
         if (index > 0) Assert.False(tst[index - 1]);
         if (index < capacity - 1) Assert.False(tst[index + 1]);
      }

      [Fact]
      public void FlipBit()
      {
         int sz = 128;
         BigBitArray tst = new BigBitArray(sz);

         for (int j = 0; j < sz; j++)
         {
            tst.FlipBit(j);
            for (int k = 0; k < sz; k ++)
            {
               if (j == k)
                  Assert.True(tst[k]);
               else
                  Assert.False(tst[k]);
            }
            tst.FlipBit(j);
         }
      }

      [Fact]
      public void Expand()
      {
         long capacity = 192;
         BigBitArray tst = new BigBitArray(capacity);

         for (long j = 0; j < capacity; j++)
            tst[j] = true;

         tst.Expand(capacity + 1);

         // Assert that new capacity is next multiple of 64
         long expectedCapacity = capacity + 64 - capacity % 64;
         Assert.Equal(expectedCapacity, tst.Capacity);

         // assert that all previous bits remain one.
         for (long j = 0; j < capacity; j++)
            Assert.True(tst[j]);

         // Assert that all newly added bits are zero.
         for (long j = capacity; j < expectedCapacity; j++)
            Assert.False(tst[j]);
      }

      [Fact]
      public void Expand_No_Shrink()
      {
         long capacity = 256;
         BigBitArray tst = new BigBitArray(capacity);

         tst.Expand(128);

         Assert.Equal(capacity, tst.Capacity);
      }

      [Fact]
      public void Xor1()
      {
         BigBitArray a = new BigBitArray(128);
         BigBitArray b = new BigBitArray(128);

         // fill a with 1010.....
         // fill b with 0101....
         for (int j = 0; j < a.Capacity; j ++)
         {
            a[j] = (j & 1) != 0;
            b[j] = (j & 1) == 0;
         }

         a.Xor(b);

         // Assert A contains all ones
         // Assert b is unchanged
         for (int j = 0; j < a.Capacity; j ++)
         {
            Assert.True(a[j]);
            Assert.Equal(b[j], (j & 1) == 0);
         }
      }

      [Fact]
      public void Xor_self()
      {
         BigBitArray a = new BigBitArray(512);
         Random random = new Random(123);

         for (int j = 0; j < 32; j++)
            a.FlipBit(random.Next((int)a.Capacity));

         a.Xor(a);

         // Assert that all bits are zero
         for (int j = 0; j < a.Capacity; j++)
            Assert.False(a[j]);
      }

      //--------------------------------------------------------------------
      [Fact]
      public void PopCount()
      {
         Random random = new Random(123);

         for (int j = 0; j < 16; j ++)
         {
            int capacity = 64 * random.Next(1, 71);
            int expectedSetBits = (capacity / 64) * random.Next(1, 10);
            BigBitArray bitVector = new BigBitArray(capacity);
            for (int k = 0; k < expectedSetBits; k++)
            {
               int bit;
               do
               {
                  bit = random.Next(0, capacity);
               } while (bitVector[bit]);
               bitVector[bit] = true;
            }

            // Assert that setBits number of bits were set
            int actualSetBits = bitVector.PopCount();
         }
      }
   }
}
