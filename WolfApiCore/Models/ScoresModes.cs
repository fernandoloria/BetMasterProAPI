namespace WolfApiCore.Models
{
    public class ReqScore
    {
        public string FileName { get; set; }
        public DateTime LoadDate { get; set; }
        public string Sport { get; set; }
        public string Country { get; set; }
        public string League { get; set; }
        public string GameDate { get; set; }
        public string HomeTeam { get; set; }
        public string HomeScore { get; set; }
        public string VisitorTeam { get; set; }
        public string VisitorScore { get; set; }
        public string Home1Q { get; set; }
        public string Visitor1Q { get; set; }
        public string Home2Q { get; set; }
        public string Visitor2Q { get; set; }
        public string Home3Q { get; set; }
        public string Visitor3Q { get; set; }
        public string Home4Q { get; set; }
        public string Visitor4Q { get; set; }
    }



    public class RespScore
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public DateTime ModifiedAt { get; set; }
    }



    public class ReqGetScores
    {
        public string Sport { get; set; }
        public string? Country { get; set; }
        public string? League { get; set; }
        public string GameDate { get; set; }
    }



    public class Team
    {
        public string homeTeam { get; set; }
        public string homeScore { get; set; }
        public string visitorTeam { get; set; }
        public string visitorScore { get; set; }
        public string home1Q { get; set; }
        public string visitor1Q { get; set; }
        public string home2Q { get; set; }
        public string visitor2Q { get; set; }
        public string home3Q { get; set; }
        public string visitor3Q { get; set; }
        public string home4Q { get; set; }
        public string visitor4Q { get; set; }
    }

    public class Game
    {
        public string country { get; set; }
        public string league { get; set; }
        public string gameDate { get; set; }
        public List<Team> teams { get; set; }
    }

    public class RespSportData
    {
        public string sport { get; set; }
        public List<Game> game { get; set; }
    }


    public class GetAllDataScore
    {
        public string Sport { get; set; }
        public string Country { get; set; }
        public string League { get; set; }
        public string GameDate { get; set; }
        public string HomeTeam { get; set; }
        public string HomeScore { get; set; }
        public string VisitorTeam { get; set; }
        public string VisitorScore { get; set; }
        public string Home1Q { get; set; }
        public string Visitor1Q { get; set; }
        public string Home2Q { get; set; }
        public string Visitor2Q { get; set; }
        public string Home3Q { get; set; }
        public string Visitor3Q { get; set; }
        public string Home4Q { get; set; }
        public string Visitor4Q { get; set; }
    }


    public class RespAllSport
    {
        public string sport { get; set; }

    }

    public class ReqFilteredLeague
    {
        public string sport { get; set; }

    }


    public class RespFilteredLeague
    {
        public string League { get; set; }
        public string Country { get; set; }

    }





}
