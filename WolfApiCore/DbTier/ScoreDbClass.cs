using Dapper;
using Microsoft.Data.SqlClient;
using WolfApiCore.Models;
using static WolfApiCore.Models.AdminModels;
using Dapper;

namespace WolfApiCore.DbTier
{
    public class ScoreDbClass
    {
        private readonly string moverConnString = "Data Source=192.168.11.36;Initial Catalog=mover;Persist Security Info=True;User ID=live;Password=d_Ez*gIb8v7NogU;TrustServerCertificate=True";

        public RespScore InsertScores(List<ReqScore> req)
        {
            RespScore resp = new RespScore()
            {
                Code = 14,
                Message = "Error completing the action, please try again.",
                ModifiedAt = DateTime.Now
            };
            DateTime tempdate = DateTime.Now;


            string sql = "exec sp_InsertScores @FileName, @LoadDate, @Sport, @Country, @League,@GameDate, @HomeTeam,@HomeScore,@VisitorTeam,@VisitorScore,@Home1Q,@Visitor1Q,@Home2Q,@Visitor2Q,@Home3Q,@Visitor3Q,@Home4Q,@Visitor4Q";
            try
            {

                if (req.Count() > 0)
                {
                    foreach (var oHiddenReq in req)
                    {
                        oHiddenReq.LoadDate = tempdate;

                        var values = new { oHiddenReq.FileName, oHiddenReq.LoadDate, oHiddenReq.Sport, oHiddenReq.Country, oHiddenReq.League, oHiddenReq.GameDate, oHiddenReq.HomeTeam, oHiddenReq.HomeScore, oHiddenReq.VisitorTeam, oHiddenReq.VisitorScore, oHiddenReq.Home1Q, oHiddenReq.Visitor1Q, oHiddenReq.Home2Q, oHiddenReq.Visitor2Q, oHiddenReq.Home3Q, oHiddenReq.Visitor3Q, oHiddenReq.Home4Q, oHiddenReq.Visitor4Q };
                        using var connection = new SqlConnection(moverConnString);
                        connection.Query<RespScore>(sql, values);
                        resp.Code = 7;
                        resp.Message = "Successful, Visibility for Sport/League Changed!";

                    }
                }
            }
            catch (Exception ex)
            {
                resp.Code = 3;
                resp.Message = ex.Message;
            }
            return resp;
        }





        public RespSportData GetScores(ReqGetScores req)
        {
            RespSportData result = new RespSportData();
            result.game = new List<Game>();
            try
            {
                string sql = "exec sp_GetScores @Sport, @Country, @League, @GameDate ";
                var values = new { Sport = req.Sport, Country = (object)req.Country, League = (object)req.League, GameDate = req.GameDate };
                //var values = new { Sport = req.Sport, Country = req.Country, League = req.League, GameDate = req.GameDate };

                using (var connection = new SqlConnection(moverConnString))
                {
                    var gameList = connection.Query<GetAllDataScore>(sql, values).ToList();


                    string lastCountry = "";
                    string lastLeague = "";
                    List<Team> teams = new List<Team>();
                    foreach (var game in gameList)
                    {
                        result.sport = game.Sport;

                        if (lastCountry == "" || lastCountry != game.Country)
                        {
                            if (lastLeague == "" || lastLeague != game.League)
                            {
                                Game games = new Game();

                                games.country = game.Country;
                                games.league = game.League;
                                games.gameDate = game.GameDate;

                                games.teams = new List<Team>();

                                foreach (var item in gameList.Where(c => c.Sport == game.Sport && c.Country == game.Country && c.League == game.League))
                                {
                                    games.teams.Add(new Team
                                    {
                                        home1Q = item.Home1Q,
                                        home2Q = item.Home2Q,
                                        home3Q = item.Home3Q,
                                        home4Q = item.Home4Q,
                                        homeScore = item.HomeScore,
                                        homeTeam = item.HomeTeam,
                                        visitor1Q = item.Visitor1Q,
                                        visitor2Q = item.Visitor2Q,
                                        visitor3Q = item.Visitor3Q,
                                        visitor4Q = item.Visitor4Q,
                                        visitorScore = item.VisitorScore,
                                        visitorTeam = item.VisitorTeam
                                    });
                                }

                                result.game.Add(games);
                                lastCountry = game.Country;
                                lastLeague = game.League;
                            }
                        }


                    }

                }
            }
            catch (Exception ex)
            {
                // Manejo de errores
            }
            return result;
        }



        public List<RespFilteredLeague> GetFilteredLeagues(ReqFilteredLeague req)
        {
            List<RespFilteredLeague> score = new List<RespFilteredLeague>();
            try
            {
                using (var connection = new SqlConnection(moverConnString))
                {
                    score = connection.Query<RespFilteredLeague>(@"
                      SELECT DISTINCT
                        [League]
                         ,[Country]
                          FROM [Mover].[dbo].[Scores] where Sport = @SportFilter", new { SportFilter = req.sport }).ToList();
                }
            }
            catch (Exception ex)
            {
                // Manejo de excepciones
            }

            return score;
        }



        public List<RespAllSport> GetAllSport()
        {
            List<RespAllSport> score = new List<RespAllSport>();
            try
            {
                using (var connection = new SqlConnection(moverConnString))
                {
                    score = connection.Query<RespAllSport>(@"
                 SELECT DISTINCT [Sport] FROM [Mover].[dbo].[Scores]").ToList();
                }
            }
            catch (Exception ex)
            {
                // Manejo de excepciones
            }

            return score;
        }





    }
}
