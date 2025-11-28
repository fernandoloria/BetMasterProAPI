namespace BetMasterApiCore.Models
{

    public class FixtureFilter: FixtureDb
    {
        public DateTime StartDate { get; set; }
        public int StatusId { get; set; }
    }
}
