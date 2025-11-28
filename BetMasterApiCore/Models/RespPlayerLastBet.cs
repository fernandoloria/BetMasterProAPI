namespace BetMasterApiCore.Models
{
    public class RespPlayerLastBet
    {
        public int idPlayer { get; set; }
        public DateTime PlacedDateTime { get; set; }
        public int HoursSinceLastBet { get; set; }
    }
}
