using Newtonsoft.Json;

namespace WolfApiCore.Models
{
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

    public class StreamModel
    {
        public List<MatchList> MatchList { get; set; }
    }
}
