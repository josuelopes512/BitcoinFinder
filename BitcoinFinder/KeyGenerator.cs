using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitcoinFinder
{
    using SimpleBase;
    using System;
    using System.Numerics;
    using System.Security.Cryptography;

    public class KeyGenerator
    {
        // Gera uma chave privada aleatória
        public byte[] GeneratePrivateKey()
        {
            byte[] privateKey = new byte[32];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(privateKey);
            }
            return privateKey;
        }

        public byte[] HexStringToByteArray(string hex)
        {
            if (hex.Length % 2 != 0)
                throw new ArgumentException("Hex string must have an even length", nameof(hex));

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }

        // Converte uma chave privada em WIF
        public string ConvertToWIF(byte[] privateKey)
        {
            byte[] extendedKey = new byte[privateKey.Length + 1];
            extendedKey[0] = 0x80; // Versão da chave privada principal da rede Bitcoin
            Buffer.BlockCopy(privateKey, 0, extendedKey, 1, privateKey.Length);

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash1 = sha256.ComputeHash(extendedKey);
                byte[] hash2 = sha256.ComputeHash(hash1);

                byte[] checksum = new byte[4];
                Buffer.BlockCopy(hash2, 0, checksum, 0, 4);

                byte[] extendedPrivateKeyWithChecksum = new byte[extendedKey.Length + 4];
                Buffer.BlockCopy(extendedKey, 0, extendedPrivateKeyWithChecksum, 0, extendedKey.Length);
                Buffer.BlockCopy(checksum, 0, extendedPrivateKeyWithChecksum, extendedKey.Length, 4);

                // Converte o resultado para Base58
                //string wif = Base58Encode(extendedPrivateKeyWithChecksum);
                string wif = Base58.Bitcoin.Encode(extendedPrivateKeyWithChecksum);

                return wif;
            }
        }

        // Converte bytes para Base58
        private string Base58Encode(byte[] data)
        {
            const string alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
            var result = new System.Text.StringBuilder();

            BigInteger value = new BigInteger(data.Reverse().ToArray(), isUnsigned: true);

            while (value > 0)
            {
                value = BigInteger.DivRem(value, 58, out BigInteger remainder);
                result.Insert(0, alphabet[(int)remainder]);
            }

            // Prepend '1' characters for leading zero bytes
            foreach (byte b in data)
            {
                if (b != 0)
                    break;

                result.Insert(0, '1');
            }

            return result.ToString();
        }
    }


    //class Program
    //{
    //    static void Main(string[] args)
    //    {
    //        KeyGenerator keyGenerator = new KeyGenerator();

    //        // Gera uma chave privada
    //        byte[] privateKey = keyGenerator.GeneratePrivateKey();

    //        // Converte a chave privada para WIF
    //        string wif = keyGenerator.ConvertToWIF(privateKey);

    //        Console.WriteLine("Chave Privada: " + BitConverter.ToString(privateKey).Replace("-", ""));
    //        Console.WriteLine("WIF: " + wif);
    //    }
    //}

}


