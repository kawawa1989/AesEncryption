using System;
using System.IO;
using UnityEngine;

namespace UnityRijndael
{
    public class Encryptor
    {
        private Matrix m_iv; // 初期化ベクトル
        private Matrix m_prevBlock; // 直前の暗号化したブロック
        private readonly BlockEncryptor m_blockEncryptor;
        private readonly byte[] m_output = new byte[BlockEncryptor.BLOCK_SIZE];

        public Encryptor(byte[] key, byte[] iv)
        {
            m_iv = new Matrix(iv);
            m_blockEncryptor = new BlockEncryptor(key, null);
        }

        public void SetEncryptionKey(byte[] key)
        {
            m_blockEncryptor.ResetRoundKeys(key);
        }

        public void Encrypt(Stream input, Stream outputStream)
        {
            int blockCount = (int)(input.Length / BlockEncryptor.BLOCK_SIZE + 1);
            int capacity = blockCount * BlockEncryptor.BLOCK_SIZE;
            int paddingCount = (int)(capacity - input.Length);
            Span<byte> inputBuffer = stackalloc byte[BlockEncryptor.BLOCK_SIZE];

            for (int i = 0; i < blockCount; ++i)
            {
                int readCount = input.Read(inputBuffer);
                int padding = BlockEncryptor.BLOCK_SIZE - readCount;
                // パディングが必要な場合、埋める
                if (padding > 0)
                {
                    for (int j = readCount; j < BlockEncryptor.BLOCK_SIZE; ++j)
                    {
                        inputBuffer[j] = (byte)paddingCount;
                    }
                }

                bool isFirstBlock = i == 0;
                // 初回である場合、IVとXORする
                if (isFirstBlock)
                {
                    m_iv.Xor(inputBuffer);
                }
                // 2回目以降は前回の暗号化したブロックとXorする
                else
                {
                    m_prevBlock.Xor(inputBuffer);
                }

                m_blockEncryptor.Encrypt(inputBuffer, m_output);
                outputStream.Write(m_output);
                m_prevBlock = new Matrix(m_output);
            }
        }
    }
}