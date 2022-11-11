using System;

namespace Friendly.Library.QuadraticSieve
{
   public class MatrixFactory : IMatrixFactory
   {
      public MatrixFactory()
      {
      }

      /// <inheritdoc />
      public IMatrix GetMatrix(int rowSize, int columnSize)
      {
         return new Matrix(rowSize, columnSize, 0);
      }
   }
}

