namespace WolfApiCore.Models
{
    public class LsportsSports
    {
        public class Body1
        {
            public List<Sport> Sports { get; set; }
        }

        public class Header1
        {
            public int HttpStatusCode { get; set; }
        }

        public class Sport
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class Root1
        {
            public Header1 Header { get; set; }
            public Body1 Body { get; set; }
        }

    }
}
