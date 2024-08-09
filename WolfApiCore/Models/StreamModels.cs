using Newtonsoft.Json;

namespace WolfApiCore.Models
{
    public class RequestStreamAccess
    {
        public int FixtureId { get; set; }
        public int IdPlayer { get; set; }
        public string HomeTeam { get; set; } = string.Empty;
        public string VisitorTeam { get; set; } = string.Empty;
        public string Sportname { get; set; } = string.Empty;
    }

    public class ResponseStreamAccess
    {
        public bool Access { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }

    

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
}
