using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace UnityRijndael.Tests
{
    public class UnityRijndaelTest
    {
        /*
         * 標準ライブラリからの暗号化
         */
        private static byte[] EncryptByDefault(byte[] key, byte[] iv, CipherMode mode, PaddingMode paddingMode,
            byte[] data, StringBuilder log)
        {
            var rijndael = Rijndael.Create();
            rijndael.KeySize = 128;
            rijndael.BlockSize = 128;
            rijndael.Mode = mode;
            rijndael.Padding = paddingMode;
            rijndael.Key = key;
            rijndael.IV = iv;
            ICryptoTransform transform = rijndael.CreateEncryptor();
            using (MemoryStream ms = new MemoryStream())
            {
                using (var cryptoStream = new CryptoStream(ms, transform, CryptoStreamMode.Write, true))
                {
                    cryptoStream.Write(data);
                }

                var encrypted = ms.ToArray();
                log.AppendLine($"--- Encrypted by Standard Library ---");
                log.AppendLine($"Encrypted Size:{encrypted.Length}");

                int blockCount = encrypted.Length / 16;
                for (int i = 0; i < blockCount; ++i)
                {
                    var block = encrypted.AsSpan(i * 16, 16);
                    log.AppendLine(new Matrix(block).ToString());
                }

                log.AppendLine();
                log.Append("key:");
                foreach (byte b in key)
                {
                    log.Append($"{b:X2}, ");
                }

                log.AppendLine();
                log.Append("iv:");
                foreach (byte b in iv)
                {
                    log.Append($"{b:X2}, ");
                }

                log.AppendLine();
                return encrypted;
            }
        }


        /// <summary>
        /// 実際には SBox は定数テーブルを利用するが、どのような計算をしているかの確認用として SBox 構造体を用意した。
        /// この SBox は値を指定すると、その値に対応した値を計算して Converted から変換後の値を取得することができる。
        /// 正しく計算が行われているのであれば定数テーブルと同じ値になるはずなので、テーブルの値と等価になっているか確認をする。
        /// </summary>
        [Test]
        public void TestSBox()
        {
            List<byte> generatedTable = new();
            for (int y = 0; y <= 0xF; ++y)
            {
                for (int x = 0; x <= 0xF; ++x)
                {
                    int value = (y << 4) | x;
                    var subByte = new SBox((byte)value);
                    generatedTable.Add(subByte.Converted);
                }
            }

            Assert.IsTrue(SBoxTable.ConvertTable.SequenceEqual(generatedTable));
        }


        [Test]
        public void TestInverseSBox()
        {
            void SBoxConvert(int value)
            {
                var box = new SBox((byte)value);
                var inverse = new SBox(box.Converted, false);
                Debug.Log($"{value} -> {box.Converted:X2} -> {inverse.Converted:X2}");
            }

            for (int i = 0; i <= 0xFF; ++i)
            {
                SBoxConvert(i);
            }
        }

        /*
         * ECBモード、パディングなし、IVなし
         * BlockEncryptor の計算結果が標準ライブラリの計算結果と同じ結果になることを期待
         */
        [Test]
        public void TestBlockEncryptor()
        {
            byte[] data = new byte[]
            {
                0x32, 0x88, 0x31, 0xe0,
                0x43, 0x5a, 0x31, 0x37,
                0xf6, 0x30, 0x98, 0x07,
                0xa8, 0x8d, 0xa2, 0x34
            };

            byte[] key = new byte[]
            {
                0x2b, 0x28, 0xab, 0x09,
                0x7e, 0xae, 0xf7, 0xcf,
                0x15, 0xd2, 0x15, 0x4f,
                0x16, 0xa6, 0x88, 0x3C
            };
            // ivは使わない
            byte[] iv = new byte[16];
            byte[] output = new byte[16];
            var log = new StringBuilder();
            var answer = EncryptByDefault(key, iv, CipherMode.ECB, PaddingMode.None, data, log);
            BlockEncryptor encryptor = new BlockEncryptor(key, new StringBuilder());
            encryptor.Encrypt(data, output);
            log.AppendLine($"--- Encrypted by UnityRijndael ---");
            log.AppendLine(new Matrix(output).ToString());
            Debug.Log(log.ToString());
            Assert.AreEqual(answer, output);
        }
        
        /*
         * ECBモード、パディングあり、IVなし
         * 
         * Pkcs7Padding の動作確認
         * 16バイトを暗号化した場合、(16 / 16 + 1) * 16 = 32バイトとなる。
         * 32 - 16 = 16であるため、padding の16バイトは全て 0x10 で埋められることが期待される。
         *
         * このテストでは0x10だけで埋めた data を暗号化するので、
         * data を暗号化した内容と 暗号化された padding 領域の内容は等しくなるはずである。
         */
        [Test]
        public void TestPkcs7Padding()
        {
            byte[] data = new byte[]
            {
                0x10, 0x10, 0x10, 0x10,
                0x10, 0x10, 0x10, 0x10,
                0x10, 0x10, 0x10, 0x10,
                0x10, 0x10, 0x10, 0x10,
            };

            byte[] key = new byte[]
            {
                0x00, 0x01, 0x02, 0x03,
                0x04, 0x05, 0x06, 0x07,
                0x08, 0x09, 0x0a, 0x0b,
                0x0c, 0x0d, 0x0e, 0x0f,
            };

            // ivは使わない
            byte[] iv = new byte[16];
            byte[] output = new byte[16];
            var log = new StringBuilder();
            var answer = EncryptByDefault(key, iv, CipherMode.ECB, PaddingMode.PKCS7, data, log);
            BlockEncryptor encryptor = new BlockEncryptor(key, new StringBuilder());
            encryptor.Encrypt(data, output);

            log.AppendLine($"--- Encrypted by UnityRijndael ---");
            log.AppendLine(new Matrix(output).ToString());
            Debug.Log(log.ToString());

            var encryptedData = answer.AsSpan(0, 16);
            var padding = answer.AsSpan(16, 16);
            Assert.AreEqual(encryptedData.ToArray(), padding.ToArray());
            Assert.AreEqual(padding.ToArray(), output.ToArray());
        }
        
        /*
         * CBCモード、パディングあり、IVあり
         * 
         * 初期化ベクトルの動作確認
         * 一番最初に暗号化するブロックの平文に対して XOR をとってからそのデータを暗号化する
         * というような使われ方をするので data と iv の XOR を行ってからそのデータを暗号化すれば
         * 同じ結果を得られるはずである。
         */
        [Test]
        public void TestIv()
        {
            byte[] data = new byte[]
            {
                0x00, 0x11, 0xcc, 0x33,
                0x44, 0xaa, 0x77, 0x66,
                0x88, 0x99, 0x55, 0xbb,
                0x22, 0xdd, 0xee, 0xff,
            };

            byte[] key = new byte[]
            {
                0x00, 0x01, 0x02, 0x03,
                0x04, 0x05, 0x06, 0x07,
                0x08, 0x09, 0x0a, 0x0b,
                0x0c, 0x0d, 0x0e, 0x0f,
            };

            byte[] iv = new byte[]
            {
                0x10, 0x21, 0x32, 0x43,
                0x54, 0x65, 0x76, 0x87,
                0x98, 0xA9, 0xBA, 0xCB,
                0xDC, 0xED, 0xFE, 0x0F,
            };

            var log = new StringBuilder();
            var answer = EncryptByDefault(key, iv, CipherMode.CBC, PaddingMode.PKCS7, data, log);

            // 平文と iv の XOR を取る
            var matrixIv = new Matrix(iv);
            matrixIv.Xor(data);

            BlockEncryptor encryptor = new BlockEncryptor(key, null);
            byte[] output = new byte[16];
            encryptor.Encrypt(data, output);

            log.AppendLine("--- Encrypted by UnityRijndael ---");
            log.AppendLine(new Matrix(output).ToString());

            var answerA = answer.AsSpan(0, 16).ToArray();
            Assert.AreEqual(answerA, output);

            // パディングは0x10で埋められているはず
            byte[] padding = 
            {
                0x10, 0x10, 0x10, 0x10,
                0x10, 0x10, 0x10, 0x10,
                0x10, 0x10, 0x10, 0x10,
                0x10, 0x10, 0x10, 0x10,
            };

            // 暗号化した内容と次の平文のXORを取り、その値を暗号化する。
            new Matrix(output).Xor(padding);
            for (int i = 0; i < output.Length; ++i)
            {
                output[i] = 0;
            }

            encryptor.Encrypt(padding, output);
            log.AppendLine(new Matrix(output).ToString());
            Debug.Log(log.ToString());

            var answerB = answer.AsSpan(16, 16).ToArray();
            Assert.AreEqual(answerB, output);
        }
    }
}