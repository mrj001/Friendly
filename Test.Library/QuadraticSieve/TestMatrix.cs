using System;
using Friendly.Library;
using Friendly.Library.QuadraticSieve;
using Xunit;

namespace Test.Library.QuadraticSieve
{
   public class TestMatrix
   {
      public TestMatrix()
      {
      }

      [Fact]
      public void ctor()
      {
         int rowCount = 64;
         int columnCount = 128;
         int bufferCount = 64;
         Matrix matrix = new Matrix(rowCount, columnCount, bufferCount);

         Assert.Equal(rowCount, matrix.Rows);
         Assert.Equal(columnCount, matrix.Columns);

         for (int r = 0; r < rowCount; r++)
            for (int c = 0; c < columnCount; c++)
               Assert.False(matrix[r, c]);
      }

      public static TheoryData<int, int, int> ExpandTestData
      {
         get
         {
            var rv = new TheoryData<int, int, int>();

            // Test where expansion fits into the buffer.
            rv.Add(32, 32, 33);

            // Test where expansion overflows the buffer.
            rv.Add(32, 32, 96);

            return rv;
         }
      }

      [Theory]
      [MemberData(nameof(ExpandTestData))]
      public void Expand(int initialColumnCount, int bufferCount, int expandedColumnCount)
      {
         int rowCount = 32;
         Matrix matrix = new Matrix(rowCount, initialColumnCount, bufferCount);

         // Initialize with a checkerboard pattern
         for (int r = 0; r < rowCount; r++)
            for (int c = 0; c < initialColumnCount; c++)
               matrix[r, c] = ((r + c) & 1) == 0;

         matrix.ExpandColumns(expandedColumnCount);

         Assert.Equal(rowCount, matrix.Rows);
         Assert.Equal(expandedColumnCount, matrix.Columns);

         // Confirm pre-existing Matrix entries are unchanged
         for (int r = 0; r < rowCount; r++)
            for (int c = 0; c < initialColumnCount; c++)
               Assert.Equal(((r + c) & 1) == 0, matrix[r, c]);

         // Confirm expanded portion of Matrix is all false
         for (int r = 0; r < rowCount; r++)
            for (int c = initialColumnCount; c < expandedColumnCount; c++)
               Assert.False(matrix[r, c]);
      }

      [Fact]
      public void Expand_Shrink_Ignored()
      {
         int rowCount = 32;
         int columnCount = 64;
         Matrix matrix = new Matrix(rowCount, columnCount, 0);

         matrix.ExpandColumns(columnCount - 1);

         Assert.Equal(columnCount, matrix.Columns);
      }

      [Fact]
      public void Indexer_Throws()
      {
         int rowCount = 64;
         int columnCount = 128;

         Matrix matrix = new Matrix(rowCount, columnCount, 0);

         Assert.Throws<ArgumentOutOfRangeException>(() => matrix[-1, 0]);
         Assert.Throws<ArgumentOutOfRangeException>(() => matrix[0, -1]);
         Assert.Throws<ArgumentOutOfRangeException>(() => matrix[rowCount, 0]);
         Assert.Throws<ArgumentOutOfRangeException>(() => matrix[0, columnCount]);

         bool bit;
         bool noThrow = true;
         try
         {
            bit = matrix[0, 0];
            bit = matrix[rowCount - 1, 0];
            bit = matrix[0, columnCount - 1];
            bit = matrix[rowCount - 1, columnCount - 1];
         }
         catch(ArgumentOutOfRangeException)
         {
            noThrow = false;
         }

         Assert.True(noThrow);
      }

      [Fact]
      public void Indexer()
      {
         int rowCount = 32;
         int columnCount = 32;

         Matrix matrix = new Matrix(rowCount, columnCount, 0);

         // Set bits one at a time stepping through the matrix.
         for (int r = 0; r < rowCount; r ++)
            for (int c = 0; c < columnCount; c ++)
            {
               matrix[r, c] = true;
               for (int r1 = 0; r1 < rowCount; r1 ++)
                  for (int c1 = 0; c1 < columnCount; c1 ++)
                     Assert.Equal(r1 < r || (r1 == r && c1 <= c), matrix[r1, c1]);
            }
      }

      [Fact]
      public void FlipBit()
      {
         int rowCount = 32;
         int columnCount = 32;

         Matrix matrix = new Matrix(rowCount, columnCount, 0);

         // Flip the bits one at a time stepping through the matrix.
         for (int r = 0; r < rowCount; r++)
            for (int c = 0; c < columnCount; c++)
            {
               matrix.FlipBit(r, c);
               for (int r1 = 0; r1 < rowCount; r1++)
                  for (int c1 = 0; c1 < columnCount; c1++)
                     Assert.Equal(r1 == r && c1 == c, matrix[r1, c1]);
               matrix.FlipBit(r, c);
               Assert.False(matrix[r, c]);
            }
      }

      [Fact]
      public void AugmentIdentity()
      {
         Random rnd = new Random(123);
         int sz = 32;
         Matrix expectedOld = new Matrix(sz, sz, 0);
         Matrix actual = new Matrix(sz, sz, sz);

         for (int j = 0; j < 100; j ++)
         {
            int r = rnd.Next(sz);
            int c = rnd.Next(sz);
            expectedOld.FlipBit(r, c);
            actual.FlipBit(r, c);
         }

         actual.AugmentIdentity();

         // Confirm pre-existing part of augmented matrix is unchanged.
         for (int r = 0; r < sz; r++)
            for (int c = 0; c < sz; c++)
               Assert.Equal(expectedOld[r, c], actual[r, c]);

         // Confirm augmented portion of the matrix is the identity
         for (int r = 0; r < sz; r++)
            for (int c = 0; c < sz; c++)
               Assert.Equal(actual[r, c + sz], r == c);
      }
   }
}
