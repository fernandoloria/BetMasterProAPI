using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using WolfApiCore.Models;
using WolfApiCore.Stream;

namespace WolfApiCore.DbTier
{
    public static class StreamDbClass
    {
        private static readonly string moverConnString = "Data Source=192.168.11.29;Initial Catalog=mover;Persist Security Info=True;User ID=live;Password=d_Ez*gIb8v7NogU;TrustServerCertificate=True";

        public static void PushNotification(BroadcastNotification notification)
        {   
            using (var connection = new SqlConnection(moverConnString))
            {
                var procedure = "[sp_MGL_PushStreamNotification]";
                var values = new
                {
                    event_id = notification.event_id,
                    sport_name = notification.sport_name,
                    league_name = notification.league_name,                         
                    team1 = notification.team1,
                    team2 = notification.team2, 
                    date = notification.date,
                    ts = notification.ts,   
                    broadcast = notification.broadcast
                };
                connection.Execute(procedure, values, commandType: CommandType.StoredProcedure);
            }
        }

        public static void DeleteNotification(BroadcastNotification notification)
        {
            using (var connection = new SqlConnection(moverConnString))
            {
                var procedure = "[sp_MGL_DeleteStreamNotification]";
                var values = new
                {
                    event_id = notification.event_id
                };
                connection.Execute(procedure, values, commandType: CommandType.StoredProcedure);
            }
        }

        public static GetStreamAccessDTO GetStreamAccess(RequestStreamAccess request)
        {
            var response = new GetStreamAccessDTO();

            try
            {
                string sql = "exec sp_MGL_StreamAccess  @idplayer ";
                var values = new { idplayer = request.IdPlayer };

                using var connection = new SqlConnection(moverConnString);

                var responseSQL = connection.Query<GetStreamAccessDTO>(sql, values).First();

                response.Access = responseSQL.Access;
                response.Message = responseSQL.Message;

                if (responseSQL.Access == true)
                {
                    string sqlStreamLinks = "exec sp_MGL_GetStreamLink @fixtureId ";
                    
                    var valuesUrlStream = new { fixtureId = request.FixtureId };
                    var streamLinks = connection.Query<StreamLinksDTO>(sqlStreamLinks, valuesUrlStream);
                    
                    if (streamLinks != null)
                    {
                        foreach ( var streamLink in streamLinks)
                        {
                            if (
                                (IsSimilarTeamName(request.homeTeam, streamLink.team1) || IsSimilarTeamName(request.homeTeam, streamLink.team2)) &&
                                (IsSimilarTeamName(request.visitorTeam, streamLink.team1) || IsSimilarTeamName(request.visitorTeam, streamLink.team2))
                                )
                            {

                                var paramSign = "?wmsAuthSign=";
                                var sign = new SignatureGenerator().GenerateSignature();
                                response.Url = streamLink.broadcast + paramSign + sign;

                            }
                        }
                    }

                }

            }
            catch (Exception ex)
            {
                response.Message = "An internal error has occurred";
                response.Access = false;
                return response;
            }

            return response;

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

    public class RequestStreamAccess
    {
        public int FixtureId { get; set; }
        public int IdPlayer { get; set; }
        public string homeTeam { get; set; } = string.Empty;
        public string visitorTeam { get; set; } = string.Empty;

    }

    public class GetStreamAccessDTO
    {
        public bool Access { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
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

}
