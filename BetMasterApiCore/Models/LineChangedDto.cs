namespace BetMasterApiCore.Models
{

    public class LineChangedDto
    {
        public int LineType { get; set; } //1 line   2 prop
        public bool LineChanged { get; set; }
        public string? Message1 { get; set; }
        public string? Message2 { get; set; }
    }
}
