using Newtonsoft.Json;

namespace BetMasterApiCore.Models
{
    public class RequestStreamAccess
    {
        public int IdPlayer { get; set; }
        public int FixtureId { get; set; }
        public string Sportname { get; set; } = string.Empty;
        public string HomeTeam { get; set; } = string.Empty;
        public string VisitorTeam { get; set; } = string.Empty;        
    }

    public class ResponseStreamAccess
    {
        public bool Access { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
}
