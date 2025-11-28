namespace BetMasterApiCore.Models
{
    public class RespPphOptionsModel
    {
            public string Page { get; set; }
            public string Title { get; set; }
            public string Order { get; set; }
            public string Type { get; set; }
            public string Value { get; set; }
            public string detail { get; set; }
    }

    public class ReqPphOptionsModel
    {
        public string? Page { get; set; }
        public string? Type { get; set; }
    }
}
