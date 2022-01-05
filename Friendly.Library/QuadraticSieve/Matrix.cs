using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Friendly.Library.QuadraticSieve
{
      /// <summary>
      /// A matrix modulo 2.
      /// </summary>
   public class Matrix
   {
      private readonly List<BigBitArray> _rows;
      private int _columns;

      /// <summary>
      /// Constructs a new Matrix object.
      /// </summary>
      /// <param name="rowSize">The number of rows in the new Matrix.</param>
      /// <param name="columnSize">The number of columns in the new Matrix.</param>
      /// <param name="columnBuffer">An allowance for optimized future expansion of the number of columns.</param>
      /// <remarks>
      /// <para>
      /// All bits are initially set to zero (false).
      /// </para>
      /// </remarks>
      public Matrix(int rowSize, int columnSize, int columnBuffer)
      {
         _rows = new List<BigBitArray>(rowSize);
         _columns = columnSize;
         for (int j = 0; j < rowSize; j++)
            _rows.Add(new BigBitArray(columnSize + columnBuffer));
      }

      /// <summary>
      /// Gets the number of Rows in the Matrix
      /// </summary>
      public int Rows { get => _rows.Count; }

      /// <summary>
      /// Gets the number of Columns in the Matrix.
      /// </summary>
      public int Columns { get => _columns; }

      /// <summary>
      /// Expands the number of Columns to the given amount.
      /// </summary>
      /// <param name="newColumnSize">The number of columns to expand to.</param>
      /// <remarks>
      /// Any attempt to shrink the number of columns is silently ignored.
      /// </remarks>
      public void ExpandColumns(int newColumnSize)
      {
         if (newColumnSize < _columns)
            return;

         if (newColumnSize < _rows[0].Capacity)
         {
            _columns = newColumnSize;
            return;
         }

         foreach (BigBitArray row in _rows)
            row.Expand(newColumnSize);
         _columns = newColumnSize;
      }

      [Conditional("DEBUG")]
      private void ValidateRow(int rowIndex)
      {
         if (rowIndex < 0 || rowIndex >= _rows.Count)
            throw new ArgumentOutOfRangeException($"{nameof(rowIndex)} = {rowIndex} is out of bounds [0, {_rows.Count})");
      }

      [Conditional("DEBUG")]
      private void ValidateColumn(int columnIndex)
      {
         if (columnIndex < 0 || columnIndex >= _columns)
            throw new ArgumentOutOfRangeException($"{nameof(columnIndex)} = {columnIndex} is out of bounds [0, {_columns}).");
      }

      /// <summary>
      /// Gets or sets the specified Bit in the Matrix
      /// </summary>
      /// <param name="rowIndex">The row to operate on.</param>
      /// <param name="columnIndex">The column to operate on.</param>
      /// <returns>True if the bit is set; false if the bit is clear.</returns>
      public bool this[int rowIndex, int columnIndex]
      {
         get
         {
            ValidateRow(rowIndex);
            ValidateColumn(columnIndex);
            return _rows[rowIndex][columnIndex];
         }
         set
         {
            ValidateRow(rowIndex);
            ValidateColumn(columnIndex);
            _rows[rowIndex][columnIndex] = value;
         }
      }

      /// <summary>
      /// Flips the specified bit.
      /// </summary>
      /// <param name="rowIndex">The row in which to flip the bit.</param>
      /// <param name="columnIndex">The column in which to flip the bit.</param>
      public void FlipBit(int rowIndex, int columnIndex)
      {
         ValidateRow(rowIndex);
         ValidateColumn(columnIndex);
         _rows[rowIndex].FlipBit(columnIndex);
      }

      /// <summary>
      /// Augments this Matrix by appending the Identity Matrix on its
      /// right hand side.
      /// </summary>
      public void AugmentIdentity()
      {
         int oldColumns = Columns;
         ExpandColumns(Columns + Rows);
         for (int r = 0, rul = Rows, c = oldColumns; r < rul; r++, c++)
            this[r, c] = true;
      }
   }
}
