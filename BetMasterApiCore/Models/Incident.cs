namespace BetMasterApiCore.Models
{


    public class Incident
    {
        public int Period { get; set; }
        public int IncidentType { get; set; }
        public int Seconds { get; set; }
        public string ParticipantPosition { get; set; }
        public List<Result> Results { get; set; }
        public string PlayerId { get; set; }
        public string PlayerName { get; set; }
        public List<int> Score { get; set; }
    }
}
