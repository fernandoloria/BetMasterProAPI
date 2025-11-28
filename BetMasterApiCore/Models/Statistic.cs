namespace BetMasterApiCore.Models
{

    public class Statistic
    {
        public int Type { get; set; }
        public List<Result> Results { get; set; }
        public List<Incident> Incidents { get; set; }
    }
}
