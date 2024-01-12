namespace WolfApiCore.Models
{
    public class LsportsLeagues
    {
        public class Body
        {
            public List<League> Leagues { get; set; }
        }

        public class Header
        {
            public int HttpStatusCode { get; set; }
        }

        public class League
        {
            public int Id { get; set; }
            public int SportId { get; set; }
            public int LocationId { get; set; }
            public string Name { get; set; }
            public string Season { get; set; }
        }

        public class Root
        {
            public Header Header { get; set; }
            public Body Body { get; set; }
        }
    }
}
