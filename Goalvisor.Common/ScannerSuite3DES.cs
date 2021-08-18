using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Goalvisor.Common
{
    public class ScannerSuite3DES
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static byte[] iv = new byte[16];

        private static string m_EncryptionKey = "DDH3kSH9SDskJDS8";
        private static string m_DecryptionKey = "DDH3kSH9SDskJDS8";

        public static string Encrypt(string r_PlainText)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(r_PlainText);
            string str;
            using (MemoryStream memoryStream = new MemoryStream(bytes.Length * 2))
            {
                ICryptoTransform encryptor = new TripleDESCryptoServiceProvider().CreateEncryptor(Encoding.ASCII.GetBytes(ScannerSuite3DES.m_EncryptionKey), ScannerSuite3DES.iv);
                using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, encryptor, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(bytes, 0, bytes.Length);
                    cryptoStream.Close();
                }
                str = ScannerSuite3DES.ToString(memoryStream.ToArray());
                memoryStream.Close();
            }
            return str;
        }

        public static string Decrypt(string r_EncryptedText)
        {
            byte[] buffer = ScannerSuite3DES.ToByteArray(r_EncryptedText);
            string @string;
            using (MemoryStream memoryStream = new MemoryStream(buffer.Length))
            {
                ICryptoTransform decryptor = new TripleDESCryptoServiceProvider().CreateDecryptor(Encoding.ASCII.GetBytes(ScannerSuite3DES.m_DecryptionKey), ScannerSuite3DES.iv);
                using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, decryptor, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(buffer, 0, buffer.Length);
                    cryptoStream.Close();
                }
                @string = Encoding.UTF8.GetString(memoryStream.ToArray());
                memoryStream.Close();
            }
            return @string;
        }

        private static string ToString(byte[] value)
        {
            StringBuilder stringBuilder = new StringBuilder(100);
            for (int index = 0; index < value.Length; ++index)
                stringBuilder.Append(value[index].ToString("x2"));
            return stringBuilder.ToString();
        }

        private static byte[] ToByteArray(string value)
        {
            if (value.Length % 2 > 0)
                throw new ApplicationException("Invalid input string. Has to be multiple of 2");
            byte[] numArray = new byte[value.Length / 2];
            int index = 0;
            int startIndex = 0;
            while (startIndex < value.Length)
            {
                string s = value.Substring(startIndex, 2);
                numArray[index] = byte.Parse(s, NumberStyles.HexNumber);
                startIndex += 2;
                ++index;
            }
            return numArray;
        }
    }
}