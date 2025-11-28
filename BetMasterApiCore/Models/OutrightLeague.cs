namespace BetMasterApiCore.Models
{

    public class OutrightLeague
    {
        public Sport Sport { get; set; }
        public Location Location { get; set; }
        public DateTime LastUpdate { get; set; }
        public int Status { get; set; }
        public List<ExtraDatum> ExtraData { get; set; }
    }
}
