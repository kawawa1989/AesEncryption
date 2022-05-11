using System;

namespace UnityRijndael
{
    // 【参考】
    // GF(2^m) のベクトル表現
    // https://tociyuki.hatenablog.jp/entry/20160114/1452758055
    // GF(q) が存在するということは GF(q^m) も存在する。これを拡大体という。
    // GF(2) でGF(4)を求めるには GF(2) で因数分解できない多項式 (既約多項式) を使う。
    // x^2 + x + 1
    //
    // リードソロモン符号
    // http://siglead.com/technology/index02.php
    // 

    /*
     * ガロア拡大体構造体
     * この構造体はガロア体 GF(2^8) を拡大する拡大体 a の計算を扱う。
     * ガロア体 GF(2) は {0, 1} の二つの要素から成る有限体であり、
     * この体に、ある方程式 f(x) = 0 の解となる値 x を付加する。
     *
     * 
     * 次に 2^8 個の元を持つガロア拡大体GF(2^8) について考える。
     * GF(2) 上の{0, 1} を係数とした8次の多項式 p(x) を考える。
     * この多項式 p(x) の根となる値 を a とし、p(a) = 0 と仮定したとき
     *
     * a^0, a^1, a^2, a^3, a^4 ... a^254
     * はすべて異なる要素となり
     * a^255 = 1
     * を成立させることができる。
     * 
     * これに0を加え、GF(2^8)の元を構成する。
     * GF(2^8) = {0, 1, a^1, a^2...a^254}
     *
     * 以降、上記の元aをxと呼ぶ
     * この元x から作られる多項式は[00000000] ~ [11111111]の範囲でビット列として表現することができる。
     * 多項式                                              2進数表記         10進数表記
     * 0                                             ... [00000000]               0
     * x^0                                           ... [00000001]               1
     * x^1                                           ... [00000010]               2
     * x^2                                           ... [00000100]               4
     * x^3                                           ... [00001000]               8
     * x^4                                           ... [00010000]              16
     * x^5                                           ... [00100000]              32
     * x^6                                           ... [01000000]              64
     * x^7                                           ... [10000000]             128
     * x^8  ...  x^4 + x^3 + x^1 + x^0               ... [00011011]              27 
     * x^9  ...  x^5 + x^4 + x^2 + x^1               ... [00110110]              54
     * x^10 ...  x^6 + x^5 + x^3 + x^2               ... [01101100]             108
     * x^11 ...  x^7 + x^6 + x^4 + x^3               ... [11011000]             216
     * x^12 ...  x^7 + x^5 + x^3 + x^1 + x^0         ... [10101011]             171
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
         * 
         * この多項式の根となる値をaと仮定したとき、
         * a^8 + a^4 + a^3 + a^1 + a^0 = 0
         * であり
         * a^8 = a^4 + a^3 + a^1 + a^0
         * と表すことができる。
         * (GF(2) のルールにおいては1 + 1 = 0であり、1 = -1 が成立するため、負の数は正の数として扱うことができる)
         */
        private const byte PrimitivePolynomial = 0x1b;

        private readonly byte _x;
        private int X0 => _x >> 0 & 1;
        private int X1 => _x >> 1 & 1;
        private int X2 => _x >> 2 & 1;
        private int X3 => _x >> 3 & 1;
        private int X4 => _x >> 4 & 1;
        private int X5 => _x >> 5 & 1;
        private int X6 => _x >> 6 & 1;
        private int X7 => _x >> 7 & 1;

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

        private byte X => _x;

        public GaloisField(int x)
        {
            _x = (byte) x;
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
                int shifted = (e << 1) ^ (((e & 0x80) != 0) ? PrimitivePolynomial : 0x00);
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