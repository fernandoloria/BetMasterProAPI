namespace BetMasterApiCore.Models
{

    public class Market
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<Provider> Providers { get; set; }
        public string MainLine { get; set; }
        public List<Bet> Bets { get; set; }
    }
}
