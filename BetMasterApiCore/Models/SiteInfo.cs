namespace BetMasterApiCore.Models
{
    public class SiteInfo
    {
        public int SiteId { get; set; }
        public string Name { get; set; }
        public string ApiUrl { get; set; }
        public byte[] SecretKey { get; set; }
        public bool IsActive { get; set; }
    }
}
