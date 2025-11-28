namespace BetMasterApiCore.Utilities
{
    public class Base64Service
    {
        public string DecodeBase64(string base64String) {
            string nullTerm = "null";

            if( string.IsNullOrEmpty(base64String) || base64String.Equals(nullTerm) ) {
                return string.Empty;
            }

            byte[] valueBytes = Convert.FromBase64String(base64String);
            return System.Text.Encoding.UTF8.GetString(valueBytes);
        }

        public bool IsBase64String(string base64)
        {
            Span<byte> buffer = new Span<byte>(new byte[base64.Length]);
            return Convert.TryFromBase64String(base64, buffer , out int bytesParsed);
        }
    }
}