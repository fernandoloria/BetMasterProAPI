namespace BetMasterApiCore.Models
{
    public class PlayerDtoStream
    {
        public int IdPlayer { get; set; }
        public string? Player { get; set; }
        public string? Password { get; set; }
        public int IdProfile { get; set; }
        public bool Access { get; set; }
    }
}
