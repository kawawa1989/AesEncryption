using System;

namespace UnityRijndael
{
    /*
     * 正方行列の列部分をまとめる構造体。
     * 
     */
    public readonly struct Word
    {
        private readonly byte m_v0;
        private readonly byte m_v1;
        private readonly byte m_v2;
        private readonly byte m_v3;

        public byte this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return m_v0;
                    case 1: return m_v1;
                    case 2: return m_v2;
                    case 3: return m_v3;
                }

                throw new ArgumentOutOfRangeException();
            }
        }

        public Word(byte value1, byte value2, byte value3, byte value4)
        {
            m_v0 = value1;
            m_v1 = value2;
            m_v2 = value3;
            m_v3 = value4;
        }

        /*
         * バイト列を循環シフトさせる
         */
        public Word RotWord()
        {
            return new Word(m_v1, m_v2, m_v3, m_v0);
        }

        /*
         * 値を SubBytes に変換した Word を生成して返す
         */
        public Word SubWord()
        {
            return new Word(
                SBoxTable.ConvertTable[m_v0],
                SBoxTable.ConvertTable[m_v1],
                SBoxTable.ConvertTable[m_v2],
                SBoxTable.ConvertTable[m_v3]
            );
        }

        public static Word operator ^(Word a, int b)
        {
            var word = new Word(
                (byte) (b & 0xFF),
                (byte) ((b >> 8) & 0xFF),
                (byte) ((b >> 16) & 0xFF),
                (byte) ((b >> 24) & 0xFF)
            );
            return a ^ word;
        }

        public static Word operator ^(Word a, Word b)
        {
            return new Word(
                (byte) (a[0] ^ b[0]),
                (byte) (a[1] ^ b[1]),
                (byte) (a[2] ^ b[2]),
                (byte) (a[3] ^ b[3])
            );
        }
    }
}