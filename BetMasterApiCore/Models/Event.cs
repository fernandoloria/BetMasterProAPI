namespace BetMasterApiCore.Models
{

    //public class Competition
    //{
    //    public int Id { get; set; }
    //    public string Name { get; set; }
    //    public int Type { get; set; }
    //    public List<Event> Events { get; set; }
    //}

    public class Event
    {
        public int FixtureId { get; set; }
        public Fixture Fixture { get; set; }
        public Livescore Livescore { get; set; }
        public List<Market> Markets { get; set; }
        public OutrightLeague OutrightLeague { get; set; }
        public OutrightFixture OutrightFixture { get; set; }
    }
}
