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
   }
}
