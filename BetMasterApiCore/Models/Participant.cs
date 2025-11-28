namespace BetMasterApiCore.Models
{

    public class Participant
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string NameToLower
        {
            get
            {
                if (Name.Contains("("))
                {
                    int index = Name.IndexOf("(");
                    var dataName = Name.Substring(0, index).Trim().ToLower();
                    return dataName;
                }
                else
                {
                    return Name.Trim().ToLower();
                }
            }
        }
        public string Position { get; set; }
        //public ExtraData ExtraData { get; set; }
        public int IsActive { get; set; }
        public List<ExtraDatum> ExtraData { get; set; }
    }
}
