namespace BetMasterApiCore.Models
{

    public class Scoreboard
    {
        public int Status { get; set; }
        public int CurrentPeriod { get; set; }
        public string Time { get; set; }
        public List<Result> Results { get; set; }
        public List<int> Score { get; set; }
    }
}
