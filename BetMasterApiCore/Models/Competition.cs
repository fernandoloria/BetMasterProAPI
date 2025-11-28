namespace BetMasterApiCore.Models
{

    //public class ExtraDatum
    //{
    //    public string Name { get; set; }
    //    public string Value { get; set; }
    //}

    public class Competition
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Type { get; set; }
        public List<Event> Events { get; set; }
    }
}
