using System.Collections.Generic;

namespace Friendly.Library.QuadraticSieve
{
   /// <summary>
   /// A modulo 2 matrix that can be used for the linear algebra stage of factoring.
   /// </summary>
   public interface IMatrix
   {
      /// <summary>
      /// Gets or sets the specified Bit in the Matrix
      /// </summary>
      /// <param name="rowIndex">The row to operate on.</param>
      /// <param name="columnIndex">The column to operate on.</param>
      /// <returns>True if the bit is set; false if the bit is clear.</returns>
      bool this[int rowIndex, int columnIndex] { get; set; }

      /// <summary>
      /// Gets the number of Rows in the Matrix
      /// </summary>
      int Rows { get; }

      /// <summary>
      /// Gets the number of Columns in the Matrix.
      /// </summary>
      int Columns { get; }

      /// <summary>
      /// Expands the number of Columns to the given amount.
      /// </summary>
      /// <param name="newColumnSize">The number of columns to expand to.</param>
      /// <remarks>
      /// Any attempt to shrink the number of columns is silently ignored.
      /// </remarks>
      void ExpandColumns(int newColumnSize);

      /// <summary>
      /// Returns a list of Column indices which specify the free variables.
      /// </summary>
      /// <returns></returns>
      List<int> FindFreeColumns();

      /// <summary>
      /// 
      /// </summary>
      /// <returns>A list of vectors satisfying the equation Ax = 0, where A represents this Matrix.</returns>
      /// <remarks>This Matrix must already have been reduced to Reduced Row Echelon Form.</remarks>
      List<BigBitArray> FindNullVectors();

      /// <summary>
      /// Performs Gauss-Jordan reduction on the Matrix.
      /// </summary>
      void Reduce();

      /// <summary>
      /// Saves the Matrix as a BitMap
      /// </summary>
      /// <param name="filename">The file to write.</param>
      void SaveBitMap(string filename);
   }
}