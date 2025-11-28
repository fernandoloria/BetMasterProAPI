using System.Security.Cryptography;
using System.Text;

namespace BetMasterApiCore.Utilities
{
    public class CryptoHelper
    {
        public static string EncryptAes(string plainText, byte[] key)
        {
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = key;
            aes.GenerateIV();

            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs, Encoding.UTF8)) 
                sw.Write(plainText);

            byte[] encrypted = ms.ToArray();
            return Base64UrlEncode(aes.IV) + "." + Base64UrlEncode(encrypted);
        }

        public static string DecryptAes(string token, byte[] key)
        {
            try
            {
                string[] parts = token.Split('.');
                if (parts.Length != 2)
                    return null;

                byte[] iv = Base64UrlDecode(parts[0]);
                byte[] cipher = Base64UrlDecode(parts[1]);

                using var aes = Aes.Create();
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = key;
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using var ms = new MemoryStream(cipher);
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr = new StreamReader(cs, Encoding.UTF8); 
                return sr.ReadToEnd();
            }
            catch
            {
                return null;
            }
        }

        private static string Base64UrlEncode(byte[] data)
        {
            return Convert.ToBase64String(data)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static byte[] Base64UrlDecode(string s)
        {
            s = s.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4)
            {
                case 2: s += "=="; break;
                case 3: s += "="; break;
            }
            return Convert.FromBase64String(s);
        }
    }
}
