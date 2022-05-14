using System;
using System.Collections.Generic;
using System.Text;

namespace UnityRijndael
{
    // https://en.wikipedia.org/wiki/Advanced_Encryption_Standard
    public class BlockEncryptor
    {
        private const int ROUND_COUNT = 10;
        private const int NB = 4;
        public const int BLOCK_SIZE = NB * 4;

        // ラウンド定数
        private static readonly byte[] s_roundConstant =
        {
            0x00000000,
            0x00000001,
            0x00000002,
            0x00000004,
            0x00000008,
            0x00000010,
            0x00000020,
            0x00000040,
            0x00000080,
            0x0000001B,
            0x00000036
        };

        private readonly List<Matrix> m_roundKeys = new(ROUND_COUNT + 1);
        private readonly StringBuilder m_logger;

        public override string ToString()
        {
            return m_logger.ToString();
        }

        public BlockEncryptor(byte[] key, StringBuilder logger)
        {
            m_logger = logger;
            ResetRoundKeys(key);
        }

        /*
         * 各ラウンドで使用される鍵を生成する。
         */
        public void ResetRoundKeys(byte[] key)
        {
            m_logger?.AppendLine("----RoundKey初期化----");
            m_roundKeys.Clear();
            // 初期ラウンドの鍵
            m_roundKeys.Add(new Matrix(key));
            for (int round = 1; round <= ROUND_COUNT; ++round)
            {
                // 一つ前のラウンドの各列をW0〜W3として考え、
                // W4〜W7を生成する。
                var prev = m_roundKeys[^1];
                var w3 = prev.W(3);

                // 一つ前のWordを取得し、バイト列をシフトしてSubBytesに変換する。
                var w4 = w3;
                w4 = w4.RotWord().SubWord() ^ s_roundConstant[round];

                w4 ^= prev.W(0);
                var w5 = w4 ^ prev.W(1);
                var w6 = w5 ^ prev.W(2);
                var w7 = w6 ^ prev.W(3);
                var matrix = new Matrix(w4, w5, w6, w7);
                m_roundKeys.Add(matrix);
            }

            if (m_logger != null)
            {
                for (int round = 0; round < m_roundKeys.Count; ++round)
                {
                    m_logger.AppendLine($"Round: {round}");
                    m_logger.AppendLine(m_roundKeys[round].ToString());
                    m_logger.AppendLine();
                }
            }

            m_logger?.AppendLine("----------------------------------------");
        }

        // input と outputのサイズは16バイトである想定
        public void Encrypt(Span<byte> input, Span<byte> output)
        {
            Span<byte> state = stackalloc byte[BLOCK_SIZE];
            input.CopyTo(state);
            
            AddRoundKey(state, 0);
            // 1〜9ラウンドまで繰り返す
            for (int round = 1; round < ROUND_COUNT; ++round)
            {
                SubBytes(state);
                ShiftRows(state);
                MixColumns(state);
                AddRoundKey(state, round);
            }

            SubBytes(state);
            ShiftRows(state);

            // 最終ラウンド
            AddRoundKey(state, ROUND_COUNT);
            state.CopyTo(output);
        }


        // S-Boxを使用してバイト列を置換する
        private void SubBytes(Span<byte> state)
        {
            m_logger?.AppendLine("----SubBytes----");
            m_logger?.AppendLine("State:");
            m_logger?.AppendLine(new Matrix(state).ToString());
            m_logger?.AppendLine("↓");
            
            for (int i = 0; i < state.Length; ++i)
            {
                state[i] = SBoxTable.ConvertTable[state[i]];
            }
            
            m_logger?.AppendLine(new Matrix(state).ToString());
            m_logger?.AppendLine();
        }

        /*
         * 4×4のバイト行列を一定規則で左シフトする
         * 1行目は操作しない
         * 2行目は1バイト
         * 3行目は2バイト
         * 4行目は3バイト
         *
         * example)
         *  0, 1, 2, 3,
         *  4, 5, 6, 7,
         *  8, 9,10,11,
         * 12,13,14,15,
         * 
         * ↓
         * 
         *  0, 1, 2, 3,
         *  4, 5, 6, 7,
         *  8, 9,10,11,
         * 12,13,14,15,
         */
        private void ShiftRows(Span<byte> state)
        {
            m_logger?.AppendLine("----ShiftRows----");
            m_logger?.AppendLine("State:");
            m_logger?.AppendLine(new Matrix(state).ToString());
            m_logger?.AppendLine("↓");
            
            // 1行目はシフトしない
            // 2行目
            {
                byte tmp1 = state[1];
                state[1] = state[5];
                state[5] = state[9];
                state[9] = state[13];
                state[13] = tmp1;
            }

            // 3行目
            {
                byte tmp2 = state[2];
                byte tmp6 = state[6];
                state[2] = state[10];
                state[6] = state[14];
                state[10] = tmp2;
                state[14] = tmp6;
            }
            
            // 4行目
            {
                byte tmp3 = state[3];
                byte tmp7 = state[7];
                byte tmp11 = state[11];
                state[3] = state[15];
                state[7] = tmp3;
                state[11] = tmp7;
                state[15] = tmp11;
            }

            m_logger?.AppendLine(new Matrix(state).ToString());
            m_logger?.AppendLine();
        }

        /*
         *
         * MixColumns ステップでは以下の行列式で各バイト列を演算する。
         * この演算はGF(2^8)のルールに基づいて演算される。
         * (足し算(+)はXORであり、掛け算(・)は多項式同士の掛け算を行う。)
         *
         * [d0]   [2 3 1 1] [b0]
         * [d1]   [1 2 3 1] [b1]
         * [d2] = [1 1 2 3] [b2]
         * [d3]   [3 1 1 2] [b3]
         *
         * d0 = (2・b0) + (3・b1) + (1・b2) + (1・b3)
         * d1 = (1・b0) + (2・b1) + (3・b2) + (1・b3)
         * d2 = (1・b0) + (1・b1) + (2・b2) + (3・b3)
         * d3 = (3・b0) + (1・b1) + (1・b2) + (2・b3)
         */
        private void MixColumns(Span<byte> state)
        {
            m_logger?.AppendLine("----MixColumns----");
            m_logger?.AppendLine("State:");
            m_logger?.AppendLine(new Matrix(state).ToString());
            m_logger?.AppendLine("↓");
            
            // バイト列を以下のように4×4の行列として扱い、各列単位で置換を行う
            //  0  1  2  3
            //  4  5  6  7
            //  8  9 10 11
            // 12 13 14 15
            for (int column = 0; column < Matrix.MATRIX_COLUMNS_WIDTH; ++column)
            {
                int i0 = Matrix.MATRIX_COLUMNS_WIDTH * column + 0;
                int i1 = Matrix.MATRIX_COLUMNS_WIDTH * column + 1;
                int i2 = Matrix.MATRIX_COLUMNS_WIDTH * column + 2;
                int i3 = Matrix.MATRIX_COLUMNS_WIDTH * column + 3;
                GaloisField b0 = state[i0];
                GaloisField b1 = state[i1];
                GaloisField b2 = state[i2];
                GaloisField b3 = state[i3];

                GaloisField d0 = (2 * b0) + (3 * b1) + (1 * b2) + (1 * b3);
                GaloisField d1 = (1 * b0) + (2 * b1) + (3 * b2) + (1 * b3);
                GaloisField d2 = (1 * b0) + (1 * b1) + (2 * b2) + (3 * b3);
                GaloisField d3 = (3 * b0) + (1 * b1) + (1 * b2) + (2 * b3);

                state[i0] = (byte) d0;
                state[i1] = (byte) d1;
                state[i2] = (byte) d2;
                state[i3] = (byte) d3;
            }

            m_logger?.AppendLine(new Matrix(state).ToString());
            m_logger?.AppendLine();
        }

        // 現在のラウンドからラウンド鍵を生成する。
        // https://www.slideshare.net/KosukeIjiri/aes-249489819
        private void AddRoundKey(Span<byte> state, int currentRound)
        {
            var roundKey = m_roundKeys[currentRound];
            
            m_logger?.AppendLine("----AddRoundKey----");
            m_logger?.AppendLine($"Round: {currentRound}");
            m_logger?.AppendLine("[RoundKey]");
            m_logger?.AppendLine(roundKey.ToString());
            m_logger?.AppendLine("State:");
            m_logger?.AppendLine(new Matrix(state).ToString());
            m_logger?.AppendLine("↓");
            
            roundKey.Xor(state);
            
            m_logger?.AppendLine(new Matrix(state).ToString());
            m_logger?.AppendLine("----------------------------------------");
        }
    }
}
