namespace BetMasterApiCore.Models
{

    public class LSport_MainLine
    {
        public LSport_ItemMainLine? SpredVisitor { get; set; }
        public LSport_ItemMainLine? SpredHome { get; set; }
        public LSport_ItemMainLine? TotalOver { get; set; }
        public LSport_ItemMainLine? TotalUnder { get; set; }
        public LSport_ItemMainLine? MlVisitor { get; set; }
        public LSport_ItemMainLine? MlHome { get; set; }
        public LSport_ItemMainLine? MlDraw { get; set; }
    }
}
