namespace BetMasterApiCore.Models
{

    public class Provider
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime LastUpdate { get; set; }
        public List<Bet> Bets { get; set; }
        public List<string> Bet { get; set; }
    }
}
