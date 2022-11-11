﻿using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Friendly.Library.QuadraticSieve
{
   /// <summary>
   /// A Matrix modulo 2, which is implemented by storing one bit per entry
   /// of the Matrix.
   /// </summary>
   public class Matrix : IMatrix
   {
      private readonly List<BigBitArray> _rows;
      private int _columns;

      private bool _rref = false;

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

      /// <inheritdoc />
      public int Rows { get => _rows.Count; }

      /// <inheritdoc />
      public int Columns { get => _columns; }

      /// <inheritdoc />
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

      /// <inheritdoc />
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

      /// <inheritdoc />
      public void Reduce()
      {
         ReduceForward();
         ReduceBackward();
         _rref = true;
      }

      /// <summary>
      /// Reduces the matrix to Row Echelon Form
      /// </summary>
      private void ReduceForward()
      {
         for (int col = 0, curRow = 0; col < Columns; col++)
         {
            if (!this[curRow, col])
            {
               // find a row below to swap with.
               int rw = curRow + 1;
               while (rw < Rows && !this[rw, col])
                  rw++;
               if (rw < Rows)
               {
                  BigBitArray t = _rows[curRow];
                  _rows[curRow] = _rows[rw];
                  _rows[rw] = t;
               }
               else
               {
                  continue;
               }
            }

            int k = curRow + 1;
            while (k < Rows)
            {
               while (k < Rows && !this[k, col])
                  k++;

               if (k < Rows)
               {
                  _rows[k].Xor(_rows[curRow]);
                  k++;
               }
            }

            curRow++;
         }
      }

      /// <summary>
      /// Performs backward substitution to achieve Reduced Row Echelon Form.
      /// </summary>
      private void ReduceBackward()
      {
         int maxCols = Columns;

         for (int row = Rows - 1; row >= 0; row--)
         {
            // Find leading non-zero coefficient.  The search starts at the
            // diagonal.
            int col = row;
            while (col < maxCols && !this[row, col])
               col++;
            if (col < maxCols)
            {
               int rw = row - 1;
               while (rw >= 0)
               {
                  while (rw >= 0 && !this[rw, col])
                     rw--;
                  if (rw >= 0)
                     _rows[rw].Xor(_rows[row]);
               }
            }
         }
      }


      /// <inheritdoc />
      public List<BigBitArray> FindNullVectors()
      {
         Assertions.True(_rref);

         int curPivotRow = 0;
         int curPivotCol = 0;
         int freeCol = 0;
         List<int> freeIndices = new List<int>();
         List<BigBitArray> nullVectors = new List<BigBitArray>();

         // Handle leading free variables.
         while (!this[0, freeCol])
         {
            BigBitArray nullVector = new BigBitArray(Columns);
            // Note: there are no bits to copy in this column.  This indicates
            // that the exponent vector components corresponding to the smallest
            // primes were all even.
            nullVector[freeCol] = true;
            nullVectors.Add(nullVector);
            freeIndices.Add(freeCol);
            freeCol++;
         }
         curPivotCol = freeCol;

         while (curPivotRow < Rows)
         {
            while (curPivotCol < Columns && !this[curPivotRow, curPivotCol])
               curPivotCol++;

            freeCol = curPivotCol + 1;
            while (freeCol < Columns && ((curPivotRow + 1 < Rows && !this[curPivotRow + 1, freeCol]) || curPivotRow == Rows - 1))
            {
               int freeIndex = 0;
               BigBitArray nullVector = new BigBitArray(Columns);

               // Proceed down this column, adding the set bits to the nullVector
               for (int j = 0; j <= curPivotRow; j++)
               {
                  while (freeIndex < freeIndices.Count && j + freeIndex == freeIndices[freeIndex])
                     freeIndex++;

                  if (this[j, freeCol])
                     nullVector[j + freeIndex] = true;
               }

               // Set the bit corresponding to this Column's free variable
               nullVector[freeCol] = true;

               nullVectors.Add(nullVector);
               freeIndices.Add(freeCol);
               freeCol++;
            }
            curPivotRow++;
            curPivotCol = freeCol;
         }

         return nullVectors;
      }

      /// <inheritdoc />
      public List<int> FindFreeColumns()
      {
         Assertions.True(_rref);

         List<int> rv = new List<int>();
         int r = 0;
         int c = 0;

         // Add leading free columns
         while (!this[r, c])
         {
            rv.Add(c);
            c++;
         }

         while (r < Rows && c < Columns)
         {
            while (c < Columns && !this[r, c])
               c++;

            c++;
            while (c < Columns && ((r + 1 < Rows && !this[r + 1, c]) || r == Rows - 1))
            {
               rv.Add(c);
               c++;
            }
            r++;
         }

         return rv;
      }
   }
}
