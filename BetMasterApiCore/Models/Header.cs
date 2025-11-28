namespace BetMasterApiCore.Models
{

    public class Header
    {
        public int Type { get; set; }
        public int MsgId { get; set; }
        public string MsgGuid { get; set; }
        public long ServerTimestamp { get; set; }
        public string CreationDate { get; set; }
    }
}
