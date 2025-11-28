namespace BetMasterApiCore.Models
{

    public class Fixture
    {
        public Sport Sport { get; set; }
        public Location Location { get; set; }
        public League League { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime OriginalDate { get; set; }
        public DateTime FilterDate
        {
            get
            {
                return new DateTime(StartDate.Year, StartDate.Month, StartDate.Day);
            }
        }
        public DateTime LastUpdate { get; set; }
        public int Status { get; set; }
        public List<Participant> Participants { get; set; }
        public List<FixtureExtraDatum> FixtureExtraData { get; set; }
    }
}
