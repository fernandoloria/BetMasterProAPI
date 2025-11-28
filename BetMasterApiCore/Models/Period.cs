namespace BetMasterApiCore.Models
{

    public class Period
    {
        public int Type { get; set; }
        public bool IsFinished { get; set; }
        public bool IsConfirmed { get; set; }
        public List<Result> Results { get; set; }
        public List<Incident> Incidents { get; set; }
        public object SubPeriods { get; set; }
        public int SequenceNumber { get; set; }
        public List<int> Score { get; set; }
    }
}
