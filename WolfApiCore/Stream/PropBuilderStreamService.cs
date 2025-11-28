using Newtonsoft.Json;
using System.Net;
using BetMasterApiCore.Models;

namespace BetMasterApiCore.Stream
{   
    public class PropBStreamModel
    {
        public List<MatchList> MatchList { get; set; }
    }
    public class MatchList
    {
        public string Count { get; set; }
        public string Live { get; set; }
        public string Sport { get; set; }
        public string League { get; set; }

        [JsonProperty("P-iddd")]
        public string Piddd { get; set; }
        public string MatchId { get; set; }
        public string T1 { get; set; }
        public string T2 { get; set; }
        public string MatchName { get; set; }
        public string Team1Image { get; set; }
        public string Team2Image { get; set; }
        public string NameSeo { get; set; }
        public string MatchDate { get; set; }
        public string MatchTime { get; set; }
        public string Embed { get; set; }
        public string FrameLink { get; set; }
    }

  
    public static class PropBuilderStreamService
    {
        public static readonly string serviceUrl = $"https://www.livemedia.services/newsource/api.php?token=VGIBTKGHFJDBSNVMCBNVBKEITOGKOSDLFKJKBVCPPF045";

        public static PropBStreamModel GetStreamList()
        {
            try
            {
                WebClient wc = new WebClient();
                string result = wc.DownloadString(serviceUrl);
                return JsonConvert.DeserializeObject<PropBStreamModel>(result);
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
