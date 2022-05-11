using System;
using System.Text;
using UnityEngine.Assertions;

namespace UnityRijndael
{
    /*
     * 鍵や入力データなどを操作する際に使用する正方行列
     * 16バイト長の正方行列に対応する形式として扱いやすいようにする。
     *   
     * w[0]:  [ 0],   [ 1],   [ 2],   [ 3],
     * w[1]:  [ 4],   [ 5],   [ 6],   [ 7],
     * w[2]:  [ 8],   [ 9],   [10],   [11],
     * w[3]:  [12],   [13],   [14],   [15],
     * 
     * 1列を1Word (4byte = 32bit) として扱う。
     * 
     * Note: FIPS197の実装例を参考にするなら 本来はWordは0,4,8,12というような縦の列を扱うべきであるようなきがするが、
     *       既存のAES暗号機能はどれも横を列として扱っているようなので、実際には上の図のように扱っている。
     */
    public unsafe struct Matrix
    {
        public const int MATRIX_COLUMNS_WIDTH = 4;
        public const int LENGTH = MATRIX_COLUMNS_WIDTH * MATRIX_COLUMNS_WIDTH;
        private fixed byte m_values[LENGTH];

        public Matrix(Word w0, Word w1, Word w2, Word w3)
        {
            SetColumn(0, w0);
            SetColumn(1, w1);
            SetColumn(2, w2);
            SetColumn(3, w3);
        }

        private void SetColumn(int rowIndex, Word w)
        {
            m_values[rowIndex * MATRIX_COLUMNS_WIDTH + 0] = w[0];
            m_values[rowIndex * MATRIX_COLUMNS_WIDTH + 1] = w[1];
            m_values[rowIndex * MATRIX_COLUMNS_WIDTH + 2] = w[2];
            m_values[rowIndex * MATRIX_COLUMNS_WIDTH + 3] = w[3];
        }

        public Matrix(Span<byte> values)
        {
            Assert.AreEqual(values.Length, LENGTH);
            for (int i = 0; i < LENGTH; ++i)
            {
                m_values[i] = values[i];
            }
        }

        public Word W(int i)
        {
            const int w = MATRIX_COLUMNS_WIDTH;
            return new Word(
                m_values[w * i + 0],
                m_values[w * i + 1],
                m_values[w * i + 2],
                m_values[w * i + 3]);
        }

        public void Xor(Span<byte> output)
        {
            for (int i = 0; i < LENGTH; ++i)
            {
                output[i] ^= m_values[i];
            }
        }
        
        public void Xor(ref Matrix output)
        {
            for (int i = 0; i < LENGTH; ++i)
            {
                output.m_values[i] ^= m_values[i];
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            for (int row = 0; row < MATRIX_COLUMNS_WIDTH; ++row)
            {
                for (int column = 0; column < MATRIX_COLUMNS_WIDTH; ++column)
                {
                    sb.Append(m_values[row * MATRIX_COLUMNS_WIDTH + column].ToString("X2"));
                    sb.Append(", ");
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}