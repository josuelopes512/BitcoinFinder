﻿using NBitcoin;
using System.Numerics;
using System.Text;

namespace BitcoinFinder
{
    public class Utils
    {
        public static BigInteger ParseHex(string value)
        {
            if (value.StartsWith("0x"))
            {
                // Remove o prefixo "0x" e converte a string para um array de bytes
                byte[] bytes = ParseHexBytes(value.Substring(2));

                BigInteger bigInteger = new BigInteger(bytes, isBigEndian: true, isUnsigned: true);

                if (bigInteger < 0)
                {
                    throw new ArgumentException("O valor não está no formato hexadecimal.");

                    //Array.Reverse(bytes);

                    //bigInteger = new BigInteger(Convert.ToUInt64(value.Substring(2), 16));

                    //if (bigInteger < 0)
                    //{
                    //    throw new ArgumentException("O valor não está no formato hexadecimal.");
                    //}
                }

                // Cria um BigInteger com o array de bytes
                return bigInteger;
            }
            else
            {
                throw new ArgumentException("O valor não está no formato hexadecimal.");
            }
        }

        public static byte[] ParseHexBytes(string hex)
        {
            // Calcula o tamanho do array de bytes baseado no tamanho da string hexadecimal
            int byteCount = (hex.Length + 1) / 2;
            byte[] bytes = new byte[byteCount];

            // Converte cada par de caracteres hexadecimais em um byte
            for (int i = 0; i < byteCount; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, Math.Min(2, hex.Length - i * 2)), 16);
            }

            return bytes;
        }

        public static string Int64ToHex(BigInteger value)
        {
            char[] hexDigits = "0123456789ABCDEF".ToCharArray();
            char[] hexChars = new char[16];

            byte[] bytes = value.ToByteArray();

            Array.Reverse(bytes);

            var testje = value.ToString("X");

            string hexString = Convert.ToHexString(bytes); //$"0x{value.ToString("X")}";

            for (int i = 15; i >= 0; i--)
            {
                int digit = (int)(value & 0xF);
                hexChars[i] = hexDigits[digit];
                value >>= 4;
            }

            var teste = new string(hexChars);

            return new string(hexChars);
        }

        public static async Task<decimal> GetBitcoinBalance(string address)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string url = $"https://blockchain.info/q/addressbalance/{address}";
                    var response = await client.GetStringAsync(url);
                    long satoshis = long.Parse(response);
                    return new Money(satoshis).ToDecimal(MoneyUnit.BTC);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao obter saldo para o endereço {address}: {ex.Message}");
                return 0;
            }
        }

        //private static string GenerateQRCode(string text)
        //{
        //    var barcodeWriter = new ZXing.BarcodeWriter
        //    {
        //        Format = BarcodeFormat.QR_CODE,
        //        Options = new ZXing.Common.EncodingOptions
        //        {
        //            Width = 250,
        //            Height = 250
        //        }
        //    };

        //    var result = barcodeWriter.Write(text);
        //    using (var ms = new System.IO.MemoryStream())
        //    {
        //        result.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        //        return Convert.ToBase64String(ms.ToArray());
        //    }
        //}

        //private static string ConvertBitMatrixToAscii(BitMatrix matrix)
        //{
        //    QRCodeWriter qRCodeWriter = new QRCodeWriter();

        //    StringBuilder sb = new StringBuilder();

        //    for (int y = 0; y < matrix.Height; y++)
        //    {
        //        for (int x = 0; x < matrix.Width; x++)
        //        {
        //            // Se o bit na posição (x, y) estiver ativado, adicione um caractere preto ('█'), caso contrário, adicione um caractere branco (' ')
        //            char character = matrix[x, y] ? '█' : ' ';
        //            sb.Append(character);
        //        }
        //        sb.AppendLine();
        //    }

        //    return sb.ToString();
        //}
    }
}
