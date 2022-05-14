using System;

namespace UnityRijndael
{
    /*
     * ガロア拡大体構造体
     * この構造体はガロア体 GF(2^8) を拡大する拡大体 a の計算を扱う。
     *
     * 
     * ガロア体 GF(2) は {0, 1} の二つの要素から成る有限体であり、
     * この体に、ある多項式 p(x) の根となる値 a を付加する。
     * {0, 1, a}
     * これをガロア拡大体と呼ぶ
     * 次に 2^8 個の元を持つガロア拡大体GF(2^8) について考える。
     * 
     * GF(2) 上の{0, 1} を係数とした8次の既約多項式 p(x) があったとする。
     * この多項式 p(x) の根となる値 を a とし、p(a) = 0 と仮定したときに
     *
     * a^0, a^1, a^2, a^3, a^4 ... a^254
     * はすべて異なる要素となり
     * a^255 = 1
     * を成立させることができる。
     * 
     * 注意: 必ずしもp(x)がこのような結果を生むとは限らない。
     * あくまでもうまくp(x)を選ぶことができた場合にのみ成立させることができる。
     * これを成立させることができる既約多項式を原始多項式と呼び、元aを原始元と呼ぶ
     * x^8 + x^4 + x^3 + x^1 + x^0 はGF(2^8)上で上記を成立させることができる原始多項式である。
     *
     * 
     * これに0を加え、GF(2^8)の元を構成する。
     * GF(2^8) = {0, a^0(=1), a^1, a^2...a^254}
     *
     * x^8 + x^4 + x^3 + x^1 + x^0 の根をaと仮定した場合、
     * a^8 = a^4 + a^3 + a^1 + a^0
     * とすることができる。
     * (GF(2) のルールにおいては1 + 1 = 0であり、1 = -1 が成立するため、負の数は正の数として扱うことができる)
     * 
     * この元a から作られる多項式は[00000000] ~ [11111111]の範囲でビット列として表現することができる。
     * 多項式                                              2進数表記         10進数表記
     * 0                                             ... [00000000]               0
     * a^0                                           ... [00000001]               1
     * a^1                                           ... [00000010]               2
     * a^2                                           ... [00000100]               4
     * a^3                                           ... [00001000]               8
     * a^4                                           ... [00010000]              16
     * a^5                                           ... [00100000]              32
     * a^6                                           ... [01000000]              64
     * a^7                                           ... [10000000]             128
     * a^8  ...  a^4 + a^3 + a^1 + a^0               ... [00011011]              27 
     * a^9  ...  a^5 + a^4 + a^2 + a^1               ... [00110110]              54
     * a^10 ...  a^6 + a^5 + a^3 + a^2               ... [01101100]             108
     * a^11 ...  a^7 + a^6 + a^4 + a^3               ... [11011000]             216
     * a^12 ...  a^7 + a^5 + a^3 + a^1 + a^0         ... [10101011]             171
     * 桁溢れしたら(つまり8ビット目の値が1でかつ、左にシフトされ、9ビット目が1になったとき)
     * 00011011 (原始多項式x^8 + x^4 + x^3 + x^1 + x^0) を 現在の値にXORをする というルールとなる。
     * 
     * 従って x^13 の値は x^12の値(10101011)を左に1ビットシフトした段階で多項式上ではx^8となり、桁溢れを起こすので
     * 00011011 を XOR することになる。
     * 10101011 << 1
     * ↓
     * 01010110 
     * 00011011(XOR
     * -----------------
     * 01001101
     * x^13 ...  x^6 + x^3 + x^2 + x^0               ... [01001101]             134
     */
    public readonly struct GaloisField
    {
        /*
         * 原始多項式
         * x^8 + x^4 + x^3 + x^1 + x^0
         * = 2進数表現で 00011011
         *   = 16進数表現で 1b
         */
        private const byte PRIMITIVE_POLYNOMIAL = 0x1b;

        private readonly byte m_x;
        private int X0 => m_x >> 0 & 1;
        private int X1 => m_x >> 1 & 1;
        private int X2 => m_x >> 2 & 1;
        private int X3 => m_x >> 3 & 1;
        private int X4 => m_x >> 4 & 1;
        private int X5 => m_x >> 5 & 1;
        private int X6 => m_x >> 6 & 1;
        private int X7 => m_x >> 7 & 1;

        public int this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return X0;
                    case 1: return X1;
                    case 2: return X2;
                    case 3: return X3;
                    case 4: return X4;
                    case 5: return X5;
                    case 6: return X6;
                    case 7: return X7;
                }

                throw new Exception("index は0~7までの値しか使えません");
            }
        }

        private byte X => m_x;

        public GaloisField(int x)
        {
            m_x = (byte) x;
        }

        /*
         * 現在の値のm乗の値を計算する
         */
        public GaloisField Pow(int m)
        {
            int source = X;
            GaloisField e = new GaloisField(X);
            for (int i = 0; i < m - 1; ++i)
            {
                e *= source;
            }

            return e;
        }


        /*
         * 加法
         * 
         * example)
         *     1. 16進数における 0x57 ... 2進数で [01010111] であり、
         *        x^6 + x^4 + x^2 + x^1 + x^0 という多項式で表現される。
         *     2. 16進数における 0x83 ... 2進数で [10000011] であり、
         *        x^7 + x^1 + x^0 で表現される。
         * また、GF(2^8) の世界における加法は1 + 1 = 0であり、XOR演算であるため、
         * 0x57 + 0x83 は実際には 0x57 ^ 0x83 であり 0xd4となる。
         */
        public static GaloisField operator +(GaloisField a, GaloisField b)
        {
            return new GaloisField((byte) (a.X ^ b.X));
        }

        // 多項式同士を掛け算する場合
        // (x^6 + x^4 + x^2 + x^1 + x^0)(x^7 + x^1 + x^0)
        // = [x^13 + x^7 + x^6] + [x^11 + x^5 + x^4] + [x^9 + x^3 + x^2] + [x^8 + x^2 + x^1] + [x^7 + x^1 + x^0]
        public static GaloisField operator *(GaloisField a, GaloisField b)
        {
            return Dot(a, b);
        }

        public static GaloisField operator *(GaloisField a, int b)
        {
            return Dot(a, new GaloisField((byte) b));
        }

        public static implicit operator GaloisField(int x)
        {
            return new GaloisField((byte) x);
        }

        public static implicit operator int(GaloisField x)
        {
            return x.X;
        }

        static GaloisField Dot(GaloisField a, GaloisField b)
        {
            int Shift(int e)
            {
                int shifted = (e << 1) ^ (((e & 0x80) != 0) ? PRIMITIVE_POLYNOMIAL : 0x00);
                return shifted;
            }

            int mask = 0x1;
            int product = 0;

            while (mask != 0)
            {
                if ((b.X & mask) != 0)
                {
                    product ^= a.X;
                }

                a = Shift(a);
                mask <<= 1;
            }

            return product;
        }
    }
}