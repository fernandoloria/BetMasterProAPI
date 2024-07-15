namespace WolfApiCore.Stream
{
    using System;
    using System.Security.Cryptography;
    using System.Text;

    public class SignatureGenerator
    {
        private readonly int CLIENT_ID = 24;
        private readonly string SECRET_KEY = "8WutOzFS6NIi3lm5";
        private readonly int VALID_MINUTES = 1;

        public string GenerateSignature()
        {
            int id = CLIENT_ID;
            int validMinutes = VALID_MINUTES;
            
            //UTC0
            //string today = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"); 
            //string today = DateTime.UtcNow.ToString("M/d/yyyy h:mm:ss tt");
            string today = DateTime.UtcNow.ToString("yyyy-MMM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);


            string key = SECRET_KEY;
            string str2hash = id + key + today + validMinutes;

            // Calcular el hash MD5
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(str2hash);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convertir el hash a base64
                string base64 = Convert.ToBase64String(hashBytes);

                // Construir la firma en el formato deseado
                string urlSignature = $"server_time={today}&hash_value={base64}&validminutes={validMinutes}&id={id}";

                // Convertir la firma a base64 
                byte[] urlSignatureBytes = Encoding.UTF8.GetBytes(urlSignature);
                string base64Signature = Convert.ToBase64String(urlSignatureBytes);

                return $"SIGNATURE: {base64Signature} HASH:{str2hash} HASH_BYTES:{hashBytes}";
            }
        }
    }
}
