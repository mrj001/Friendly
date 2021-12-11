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
   }
}
