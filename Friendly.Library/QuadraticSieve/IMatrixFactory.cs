using System;

namespace Friendly.Library.QuadraticSieve
{
   public interface IMatrixFactory
   {
      /// <summary>
      /// Creates a new IMatrix object with the specified size.
      /// </summary>
      /// <param name="rowSize">The number of Rows in the new IMatrix.</param>
      /// <param name="columnSize">The number of Columns in the new IMatrix.</param>
      /// <returns>A newly created IMatrix implementation.</returns>
      IMatrix GetMatrix(int rowSize, int columnSize);
   }
}

