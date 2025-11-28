namespace BetMasterApiCore.Models
{

    public class FixtureParticipanTable
    {
        public int FixtureId { get; set; }
        public int ParticipantId { get; set; }
        public string Name { get; set; }
        public int Position { get; set; }
        public int Rot { get; set; }
    }
}
