using Newtonsoft.Json;
using System.Net;
using WolfApiCore.DbTier;
using WolfApiCore.Models;

namespace WolfApiCore.Stream
{
    public static class EzStreamService
    {
        private static readonly string EzStreamListURL = "https://api-wolf.player-us.xyz/stream-list-v2/?tv=usa";
        private static readonly string EzStreamEventURL = "https://wolf.player-us.xyz/tv/?stream_id=";

        public static string getEzStream(RequestStreamAccess request)
        {
            var streamList = getEzStreamList();
            var URL = "";

            if (request.Sportname == "Football")
                request.Sportname = "american_football";
            else if (request.Sportname == "Soccer")
                request.Sportname = "Football";
            else
                request.Sportname = request.Sportname.Replace(" ", "_");

            // buscar el deporte
            var sport = streamList.Sports.Where(s => s.Key.ToUpper() == request.Sportname.ToUpper()).FirstOrDefault();

            if (sport.Key != null && sport.Value.Events.Count > 0)
            {
                var participants = new LiveDbClass().GetParticipants(request.FixtureId);

                foreach (var sportEvent in sport.Value.Events)
                {
                    var encontro = false;
                    var game = sportEvent.Value;
                    var homeTeam = game.competitiors.Home;
                    var visitorTeam = game.competitiors.Away;

                    // Buscar por RotNumber
                    if (participants.Any(p => p.Rot != 0 && p.Rot == int.Parse(game.Donbest_Id)))
                        encontro = true;
                    // Busqueda por Nombre
                    else
                    {
                        encontro =
                            // que el home sea similar
                            (IsSimilarTeamName(request.HomeTeam, homeTeam) ||
                             IsSimilarTeamName(request.HomeTeam, visitorTeam))
                            &&
                            // y el visitor tambien
                            (IsSimilarTeamName(request.VisitorTeam, visitorTeam) ||
                             IsSimilarTeamName(request.VisitorTeam, homeTeam));
                    }

                    if (encontro)
                    {
                        // guarda el URL q encontro
                        URL = EzStreamEventURL + game.Stream_Id;

                        if (isAlternative(homeTeam, visitorTeam))
                            continue; // si era un alternative, continuar a ver si encuentra el principal
                        else
                            break; // si es el principal retornar
                    }
                }
            }
            return URL;
        }

        private static bool isAlternative(string homeTeam, string visitorTeam) 
        {
            return ((homeTeam + visitorTeam).ToLower().Contains("(alt)"));
        }

        private static EzStreamModel getEzStreamList()
        {
            try
            {
                WebClient wc = new WebClient();
                string result = wc.DownloadString(EzStreamListURL);
                return JsonConvert.DeserializeObject<EzStreamModel>(result);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private static IEnumerable<string> GenerateNGrams(string text, int nGramLength)
        {
            for (int i = 0; i <= text.Length - nGramLength; i++)
            {
                yield return text.Substring(i, nGramLength);
            }
        }

        private static bool IsSimilarTeamName(string team1, string team2, int nGramLength = 2, double similarityThreshold = 0.5)
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
    }
}