namespace BetMasterApiCore.Models
{

    public class Livescore
    {
        public Scoreboard Scoreboard { get; set; }
        public List<Period> Periods { get; set; }
        public List<LivescoreExtraDatum> LivescoreExtraData { get; set; }
        public List<Statistic> Statistics { get; set; }
    }
}
