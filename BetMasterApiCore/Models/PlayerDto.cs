namespace BetMasterApiCore.Models
{
    public class PlayerDto
    {
        public int IdPlayer { get; set; }
        public string? Player { get; set; }
        public int IdProfile { get; set; }
        public bool Access { get; set; }
    }
}
