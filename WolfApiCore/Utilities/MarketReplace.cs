using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using WolfApiCore.DbTier;
using WolfApiCore.Models;

namespace WolfApiCore.Utilities
{   
    public class MarketReplace
    {
        // markets que se debe buscar el 1 X 2 en todo el texto
        public static List<int> MarketsReplaceAll1X2 = new List<int> { };

        // markets que nunca debe reeemplzar el 1 X 2
        public static List<int> MarketsReplaceNever1X2 = new List<int> { };

        int marketId = 0;
        string name = string.Empty;
        string homeTeam = string.Empty;
        string visitorTeam = string.Empty;

        public MarketReplace(int marketId, string name, string homeTeam, string visitorTeam)
        {
            this.marketId = marketId;
            this.name = name;
            this.homeTeam = homeTeam;
            this.visitorTeam = visitorTeam;
        }

        
        public static void LoadMarketsList(LiveDbClass liveDb)
        {
            // cargar de la bd los markets q se usan para los replaces
            MarketsReplaceAll1X2 = liveDb.GetMarketReplaceAll1x2();
            MarketsReplaceNever1X2 = liveDb.GetMarketReplaceNever1x2();
        }

        public static string ReplaceWholeWord(string input, string wordToReplace, string replacement)
        {
            // Patrón para encontrar la palabra completa (wordToReplace) como palabra separada
            string pattern = @"\b" + Regex.Escape(wordToReplace) + @"\b";

            // Realizar el reemplazo usando Regex.Replace
            string replaced = Regex.Replace(input, pattern, replacement);

            return replaced;
        }

        string ReplaceAll1X2()
        {
            name = name.Replace("1", " " + homeTeam +" ");// ReplaceWholeWord(name, "1", " " + homeTeam +" ");
            name = name.Replace("X", " DRAW ");// ReplaceWholeWord(name, "X", " DRAW ");
            name = name.Replace("2", " " + visitorTeam +" ");// ReplaceWholeWord(name, "2", " " + visitorTeam +" ");

            return name;
        }

        string MarketReplaceSimple1X2() 
        {
            return name == "1" ? homeTeam
                 : name == "2" ? visitorTeam
                 : name == "X" ? "DRAW"
                 : name;
        }

        public string MarketReplace1X2()
        {
            name = name.ToUpper();

            return  MarketsReplaceAll1X2.Contains(marketId) ? ReplaceAll1X2()
                  : MarketsReplaceNever1X2.Contains(marketId) ? name
                  : MarketReplaceSimple1X2();
        }
        
    }
}
