namespace UnityRijndael
{
    public struct SBox
    {
        private static readonly GaloisField s_constant = 0x63; // 01100011
        private static readonly GaloisField s_inverseConstant = 0x5; // 00000101

        private byte? m_converted;
        private readonly int m_input;
        private readonly bool m_forward;

        public byte Converted
        {
            get { return m_converted ??= (byte) Convert(m_input, m_forward); }
        }

        public SBox(byte input, bool forward = true)
        {
            m_input = input;
            m_forward = forward;
            m_converted = null;
        }

        private int Convert(int input, bool forward)
        {
            if (forward)
            {
                return Forward(input);
            }

            return Inverse(input);
        }

        private int Forward(int input)
        {
            GaloisField gf = new(input);
            /*
             *
             *  【逆数の求め方】
             *   ガロア体の要素【a】は
             *   a^(p^m-1) = 1
             *   となる性質がある。
             * 
             *   したがって
             *   a^-1 * a^255 = a^254
             *   となる a^254 を求めれば良い
             */
            GaloisField b = gf.Pow(254);
            GaloisField c = s_constant;

            /*
             *
             * 定数63（16）＝01100011（2）は16進数で示される。
             * アフィン変換の等価式は次の通りである。
             * このアフィン変換は、バイトをベクトルとして複数回回転させたものの和であり、加算はXOR演算となる。
             * 
             * アフィン変換式
             * s[i] = b[i] + b[(i + 4) % 8] + b[(i + 5) % 8] + b[(i + 6) % 8] + b[(i + 7) % 8] + c[i]
             * 
             * 従って、以下のようになる。
             * s[0] = b[0] + b[4] + b[5] + b[6] + b[7] + c[0]
             * s[1] = b[0] + b[1] + b[5] + b[6] + b[7] + c[1]
             * s[2] = b[0] + b[1] + b[2] + b[6] + b[7] + c[2]
             * s[3] = b[0] + b[1] + b[2] + b[3] + b[7] + c[3]
             * s[4] = b[0] + b[1] + b[2] + b[3] + b[4] + c[4]
             * s[5] = b[1] + b[2] + b[3] + b[4] + b[5] + c[5]
             * s[6] = b[2] + b[3] + b[4] + b[5] + b[6] + c[6]
             * s[7] = b[3] + b[4] + b[5] + b[6] + b[7] + c[7]
             */
            int s0 = b[0] ^ b[4] ^ b[5] ^ b[6] ^ b[7] ^ c[0];
            int s1 = b[0] ^ b[1] ^ b[5] ^ b[6] ^ b[7] ^ c[1];
            int s2 = b[0] ^ b[1] ^ b[2] ^ b[6] ^ b[7] ^ c[2];
            int s3 = b[0] ^ b[1] ^ b[2] ^ b[3] ^ b[7] ^ c[3];
            int s4 = b[0] ^ b[1] ^ b[2] ^ b[3] ^ b[4] ^ c[4];
            int s5 = b[1] ^ b[2] ^ b[3] ^ b[4] ^ b[5] ^ c[5];
            int s6 = b[2] ^ b[3] ^ b[4] ^ b[5] ^ b[6] ^ c[6];
            int s7 = b[3] ^ b[4] ^ b[5] ^ b[6] ^ b[7] ^ c[7];

            return (byte) (s0 | s1 << 1 | s2 << 2 | s3 << 3 | s4 << 4 | s5 << 5 | s6 << 6 | s7 << 7);
        }
        
        private int Inverse(int input)
        {
            GaloisField s =  new(input);
            GaloisField c = s_inverseConstant;

            /*
             *
             * 定数63（16）＝01100011（2）は16進数で示される。
             * アフィン変換の等価式は次の通りである。
             * このアフィン変換は、バイトをベクトルとして複数回回転させたものの和であり、加算はXOR演算となる。
             * 
             * 従って、以下のようになる。
             */
            int b0 = s[2] ^ s[5] ^ s[7] ^ c[0];
            int b1 = s[0] ^ s[3] ^ s[6] ^ c[1];
            int b2 = s[1] ^ s[4] ^ s[7] ^ c[2];
            int b3 = s[0] ^ s[2] ^ s[5] ^ c[3];
            int b4 = s[1] ^ s[3] ^ s[6] ^ c[4];
            int b5 = s[2] ^ s[4] ^ s[7] ^ c[5];
            int b6 = s[0] ^ s[3] ^ s[5] ^ c[6];
            int b7 = s[1] ^ s[4] ^ s[6] ^ c[7];
            
            GaloisField gf = new ((byte) (b0 | b1 << 1 | b2 << 2 | b3 << 3 | b4 << 4 | b5 << 5 | b6 << 6 | b7 << 7));
            return gf.Pow(254);
        }
    }
}