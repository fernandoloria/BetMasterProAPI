using Dapper;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;
using System.Net;
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
                    var integracionRusos = false;
                    var integracionEZ = true;

                    if (integracionRusos)
                    {
                        string sqlStreamLinks = "exec sp_MGL_GetStreamLink @fixtureId ";

                        var valuesUrlStream = new { fixtureId = request.FixtureId };
                        var streamLinks = connection.Query<StreamLinksDTO>(sqlStreamLinks, valuesUrlStream);

                        if (streamLinks != null)
                        {
                            foreach (var streamLink in streamLinks)
                            {
                                if (
                                    (IsSimilarTeamName(request.HomeTeam, streamLink.team1) || IsSimilarTeamName(request.HomeTeam, streamLink.team2)) &&
                                    (IsSimilarTeamName(request.VisitorTeam, streamLink.team1) || IsSimilarTeamName(request.VisitorTeam, streamLink.team2))
                                    )
                                {

                                    var paramSign = "?wmsAuthSign=";
                                    var sign = new SignatureGenerator().GenerateSignature();
                                    response.Url = streamLink.broadcast + paramSign + sign;

                                }
                            }
                        }
                    }
                    else if (integracionEZ)
                    {
                        var streamList = getEzStreamList();
                        var participants = GetParticipantsByFixture(request.FixtureId);

                        if (request.Sportname == "Football")
                        {
                            request.Sportname = "american_football";
                        }
                        if (request.Sportname == "Soccer")
                        {
                            request.Sportname = "Football";
                        }

                        request.Sportname = request.Sportname.Replace(" ", "_");

                        foreach (var sport in streamList.Sports) //recorre la lista de deportes
                        {
                            if (sport.Key.ToUpper() == request.Sportname.ToUpper())
                            {
                                var sportEvents = sport.Value.Events;

                                foreach (var events in sportEvents) //recorre los eventos del deporte
                                {
                                    var game = events.Value;
                                    var homeTeam = game.competitiors.Home;
                                    var visitorTeam = game.competitiors.Away;

                                    //busqueda por rotNumber
                                    foreach (var participant in participants)
                                    {
                                        if (participant.Rot !=0 && participant.Rot == int.Parse(game.Donbest_Id))
                                        {
                                            response.Url = $"https://wolf.player-us.xyz/tv/?stream_id={game.Stream_Id}";
                                            return response;
                                        }
                                    }

                                    //busqueda por nombre
                                    if ( IsSimilarTeamNameEZ(request.HomeTeam, homeTeam) || IsSimilarTeamNameEZ(request.VisitorTeam, visitorTeam) ||
                                        IsSimilarTeamNameEZ(request.VisitorTeam, homeTeam) || IsSimilarTeamNameEZ(request.HomeTeam, visitorTeam)
                                    )
                                    {
                                        response.Url = $"https://wolf.player-us.xyz/tv/?stream_id={game.Stream_Id}";
                                        return response;
                                    }

                                }

 
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

        public static List<ResponseGetParticipants> GetParticipantsByFixture(int fixtureId)
        {
            try
            {
                var valuesSp = new { FixtureId = fixtureId };
                using var connection = new SqlConnection(moverConnString);
                string sqlGetParticipants = "exec sp_Live_GetParticipants @FixtureId ";

                var participant = connection.Query<ResponseGetParticipants>(sqlGetParticipants, valuesSp);

                List<ResponseGetParticipants> response = new List<ResponseGetParticipants>();
                foreach (var item in participant)
                {
                    response.Add(item);
                }

                return response;

            }
            catch(Exception ex) 
            {
                throw new Exception("An error occurred to get participants");
            }

        }

        public static EzStreamModel getEzStreamList()
        {
            try
            {
                var serviceUrl = "https://api-wolf.player-us.xyz/stream-list-v2/?tv=usa"; 
                WebClient wc = new WebClient();
                string result = wc.DownloadString(serviceUrl);

                var list = JsonConvert.DeserializeObject<EzStreamModel>(result);

                return list;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        //ez
        public static IEnumerable<string> GenerateNGrams(string text, int nGramLength)
        {
            for (int i = 0; i <= text.Length - nGramLength; i++)
            {
                yield return text.Substring(i, nGramLength);
            }
        }

        //ez
        public static bool IsSimilarTeamNameEZ(string team1, string team2, int nGramLength = 2, double similarityThreshold = 0.5)
        {
            // Genera n-gramas para ambos textos
            var nGramsFirstText = new HashSet<string>(GenerateNGrams(team1.ToLower().Trim(), nGramLength));
            var nGramsSecondText = new HashSet<string>(GenerateNGrams(team2.ToLower().Trim(), nGramLength));

            // Calcula la intersección y la unión de los conjuntos de n-gramas
            var commonNGrams = nGramsFirstText.Intersect(nGramsSecondText).Count();
            var allNGrams = nGramsFirstText.Union(nGramsSecondText).Count();

            // Calcula la similitud como el cociente de la intersección sobre la unión
            double similarity = (double)commonNGrams / allNGrams;

            // Devuelve true si la similitud supera el umbral, false en caso contrario
            return similarity >= similarityThreshold;
        }

        //rusos
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
        //rusos
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

    public class ResponseGetParticipants
    {
        public int ParticipantId { get; set; }
        public string Name { get; set; }
        public int Position { get; set; }
        public int Rot { get; set; }
    }

    public class RequestStreamAccess
    {
        public int FixtureId { get; set; }
        public int IdPlayer { get; set; }
        public string HomeTeam { get; set; } = string.Empty;
        public string VisitorTeam { get; set; } = string.Empty;
        public string Sportname { get; set; } = string.Empty;

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
