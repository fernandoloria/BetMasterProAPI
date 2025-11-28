namespace BetMasterApiCore.Models
{

    public class Body
    {
        public int FixtureId { get; set; }
        public Fixture Fixture { get; set; }
        public Livescore Livescore { get; set; }
        public List<Market> Markets { get; set; }
        public List<Event> Events { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public int LocationId { get; set; }
        public int SportId { get; set; }
        public string Season { get; set; }
        public int Type { get; set; }
        public Competition Competition { get; set; }
        public List<Competition> Competitions { get; set; }
    }
}
