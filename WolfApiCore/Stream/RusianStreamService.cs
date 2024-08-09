using Newtonsoft.Json;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using WolfApiCore.DbTier;
using WolfApiCore.Models;

namespace WolfApiCore.Stream
{
    public class BroadcastNotification
    {
        public string sport_name { get; set; }
        public string league_name { get; set; }
        public int event_id { get; set; }
        public string team1 { get; set; }
        public string team2 { get; set; }
        public string date { get; set; }
        public int ts { get; set; }
        public string broadcast { get; set; }
        public int type { get; set; }
    }

    public class StreamLinksDTO
    {
        public int event_id { get; set; }
        public string sport_name { get; set; } = string.Empty;
        public string league_name { get; set; } = string.Empty;
        public string team1 { get; set; } = string.Empty;
        public string team2 { get; set; } = string.Empty;
        public DateTime event_date { get; set; }
        public int time_stamp { get; set; }
        public string broadcast { get; set; } = string.Empty;
        public int fixtureId { get; set; }
        public int type { get; set; }
        public bool active { get; set; }
    }

    public class RusianStreamService
    {

        private static  readonly int CLIENT_ID = 24;
        private static readonly string SECRET_KEY = "8WutOzFS6NIi3lm5";
        private static readonly int VALID_MINUTES = 1;

        public static string GenerateSignature()
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

                return base64Signature;
            }
        }

        public static string getRussianStream(RequestStreamAccess request)
        {
            string URL = "";
            var streamLinks = StreamDbClass.getStreamLinks(request.FixtureId);

            foreach (var streamLink in streamLinks)
            {
                if (
                (IsSimilarTeamName(request.HomeTeam, streamLink.team1) || IsSimilarTeamName(request.HomeTeam, streamLink.team2)) &&
                (IsSimilarTeamName(request.VisitorTeam, streamLink.team1) || IsSimilarTeamName(request.VisitorTeam, streamLink.team2))
                )
                {
                    var paramSign = "?wmsAuthSign=";
                    var sign = GenerateSignature();
                    URL = streamLink.broadcast + paramSign + sign;
                }
            }

            return URL;
        }

        public static void PushNotification(BroadcastNotification notification)
        {
            switch (notification.type)
            {
                case 1:
                case 2:
                    StreamDbClass.PushNotification(notification); //insert, update
                    break;

                case 3:
                    StreamDbClass.DeleteNotification(notification);//delete
                    break;

                default: throw new Exception("Unknown notification type");
            }
        }

        private static bool IsSimilarTeamName(string teamname, string other)
        {
            if (string.IsNullOrWhiteSpace(teamname) || string.IsNullOrWhiteSpace(other))
                return false;

            // Normaliza las cadenas
            teamname = teamname.Trim().ToLower();
            other = other.Trim().ToLower();

            // Calcula la distancia de Levenshtein
            int distance = LevenshteinDistance(teamname, other);
            double maxLength = Math.Max(teamname.Length, other.Length);

            // Devuelve true si la similitud es suficiente (menos de 40% de diferencia)
            return (double)distance / maxLength < 0.4;
        }
        
        private static int LevenshteinDistance(string a, string b)
        {
            int[,] matrix = new int[b.Length + 1, a.Length + 1];

            for (int i = 0; i <= b.Length; i++)
            {
                matrix[i, 0] = i;
            }

            for (int j = 0; j <= a.Length; j++)
            {
                matrix[0, j] = j;
            }

            for (int i = 1; i <= b.Length; i++)
            {
                for (int j = 1; j <= a.Length; j++)
                {
                    int cost = (a[j - 1] == b[i - 1]) ? 0 : 1;
                    matrix[i, j] = Math.Min(Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1), matrix[i - 1, j - 1] + cost);
                }
            }

            return matrix[b.Length, a.Length];
        }
    }
}
