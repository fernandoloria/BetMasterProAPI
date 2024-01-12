using Microsoft.Data.SqlClient;
using static WolfApiCore.Models.AdminModels;
using System.Data;
using WolfApiCore.Models;
using Dapper;

namespace WolfApiCore.DbTier
{
    public class PropBuildDbClass
    {
        private readonly string connString;

        public PropBuildDbClass(string ConnString)
        {
            connString = ConnString;
        }

        private List<LSportGameDto> GetAllActiveEvents()
        {
            List<LSportGameDto> resultList = new List<LSportGameDto>();
            string sql = "exec [sp_MGL_GetActiveEvents_pb]";
            // var values = new { StatusId = StatusId };

            try
            {
                using var connection = new SqlConnection(connString);
                resultList = connection.Query<LSportGameDto>(sql/*, values*/).ToList();

                foreach (var game in resultList)
                {
                    //get markets/lines
                    game.PropMarkets = ConvertToScreenProps(GetEventsMarketsLines(game.FixtureId, game.SportId), game.FixtureId);

                }
            }
            catch (Exception ex)
            {
                //here todo log
                string value = ex.Message;
            }

            return resultList;
        }//end GetAllScores

        private List<LSportGameDto> GetSignalEvents()
        {
            List<LSportGameDto> resultList = new List<LSportGameDto>();
            string sql = "exec [sp_MGL_GetSignalEvents_pb]";
            // var values = new { StatusId = StatusId };

            try
            {
                using var connection = new SqlConnection(connString);
                resultList = connection.Query<LSportGameDto>(sql/*, values*/).ToList();


                foreach (var game in resultList)
                {
                    //get markets/lines
                    game.PropMarkets = ConvertToScreenProps(GetEventsMarketsLines(game.FixtureId, game.SportId), game.FixtureId);

                }
            }
            catch (Exception ex)
            {
                //here todo log
                string value = ex.Message;
            }

            return resultList;
        }//end GetAllScores

        public LSportGameDto GetEventByFixture(int FixtureId)
        {
            LSportGameDto resultGame = new LSportGameDto();
            string sql = "exec sp_MGL_GetEventsByFixture @FixtureId";
            var values = new { FixtureId = FixtureId };

            try
            {
                using var connection = new SqlConnection(connString);
                resultGame = connection.Query<LSportGameDto>(sql, values).FirstOrDefault();
                if (resultGame != null)
                    resultGame.PropMarkets = ConvertToScreenProps(GetEventsMarketsLines(FixtureId, resultGame.SportId), resultGame.FixtureId);

            }
            catch (Exception ex)
            {
                //here todo log
                string value = ex.Message;
            }

            return resultGame;
        }//end GetAllScores

        public List<LSport_ScreenSportsDto> GetGamesAndLines(int idPlayer)
        {
            List<LSport_ScreenSportsDto> EventSportLines = new List<LSport_ScreenSportsDto>();
            try
            {
                //var GameList = GetAllEventsByOur(hours); //obtemenos todos los fixtures de las ultimas 3 horas
                LiveAdminDbClass oLiveAdmin = new LiveAdminDbClass();
                LiveDbWager oLiveDbWager = new LiveDbWager();
                var GameList = GetAllActiveEvents();
                if (idPlayer > 0)
                {
                    var oPlayerHierarchy = oLiveDbWager.GetPlayerHierarchy(idPlayer);
                    List<GetSportsAndLeaguesHiddenReq> oListResp = new List<GetSportsAndLeaguesHiddenReq>();
                    GetSportsAndLeaguesHiddenReq resPlayer = new GetSportsAndLeaguesHiddenReq()
                    {
                        AgentId = oPlayerHierarchy.SubAgentId,
                        PlayerId = oPlayerHierarchy.PlayerId

                    };
                    oListResp.Add(resPlayer);
                    var oSportsAndLeaguesBlockedPlayer = oLiveAdmin.GetSportsAndLeaguesHidden(oListResp).SelectMany(item => new[] { item.SportId, item.LeagueId }).Distinct();
                    GameList = GameList.Where(game => !oSportsAndLeaguesBlockedPlayer.Contains(game.SportId) && !oSportsAndLeaguesBlockedPlayer.Contains(game.LeagueId)).ToList();
                    oListResp.RemoveAt(0);
                    GetSportsAndLeaguesHiddenReq resSubAgent = new GetSportsAndLeaguesHiddenReq()
                    {
                        AgentId = oPlayerHierarchy.SubAgentId,
                        PlayerId = null

                    };
                    oListResp.Add(resSubAgent);
                    var oSportsAndLeaguesBlockedSubAgent = oLiveAdmin.GetSportsAndLeaguesHidden(oListResp).SelectMany(item => new[] { item.SportId, item.LeagueId }).Distinct();
                    GameList = GameList.Where(game => !oSportsAndLeaguesBlockedSubAgent.Contains(game.SportId) && !oSportsAndLeaguesBlockedSubAgent.Contains(game.LeagueId)).ToList();
                    oListResp.RemoveAt(0);
                    GetSportsAndLeaguesHiddenReq resMaster = new GetSportsAndLeaguesHiddenReq()
                    {
                        AgentId = oPlayerHierarchy.MasterAgentId,
                        PlayerId = null
                    };
                    oListResp.Add(resMaster);
                    var oSportsAndLeaguesBlockedMaster = oLiveAdmin.GetSportsAndLeaguesHidden(oListResp).SelectMany(item => new[] { item.SportId, item.LeagueId }).Distinct();
                    GameList = GameList.Where(game => !oSportsAndLeaguesBlockedMaster.Contains(game.SportId) && !oSportsAndLeaguesBlockedMaster.Contains(game.LeagueId)).ToList();
                    oListResp.RemoveAt(0);

                    foreach (var game in GameList)
                    {
                        var oGameDGS = GetInfoPrematchDGS(game.FixtureId);
                        if (oGameDGS != null)
                        {
                            var count = GetTotalWagersByGame(oGameDGS.IdGame);
                            if (count != null)
                            {
                                game.BetCount = count.WagersCount;
                                game.PremiumGame = count.WagersCount >= 500 ? true : false;
                            }
                            else
                            {
                                game.BetCount = 0;
                                game.PremiumGame = false;
                            }
                        }


                        if (game.TotalLines > 0)
                        {
                            var teams = GetParticipants(game.FixtureId);
                            if (teams != null)
                            {
                                game.HomeTeam = teams.Where(x => x.Position == 1).FirstOrDefault()?.Name;
                                game.HomeTeamId = teams.Where(x => x.Position == 1).FirstOrDefault() != null
                                    ? Convert.ToInt32(teams.Where(x => x.Position == 1).FirstOrDefault().ParticipantId)
                                    : 0;
                                game.VisitorTeam = teams.Where(x => x.Position == 2).FirstOrDefault()?.Name;
                                game.VisitorTeamId = teams.Where(x => x.Position == 2).FirstOrDefault() != null
                                    ? Convert.ToInt32(teams.Where(x => x.Position == 2).FirstOrDefault().ParticipantId)
                                    : 0;
                            }

                            //check if this sports already exists in EventSportLines
                            if (EventSportLines.Where(x => x != null && x.SportId == game.SportId).Any())
                            {
                                //yes, sport exists
                                if (EventSportLines.Where(x => x != null && x.SportId == game.SportId).FirstOrDefault()
                                    .Leagues.Where(f => f.LeagueId == game.LeagueId).Any())
                                {
                                    EventSportLines.Where(x => x != null && x.SportId == game.SportId).FirstOrDefault()
                                        .Leagues.Where(f => f.LeagueId == game.LeagueId).FirstOrDefault().Games.Add(game);
                                }
                                else
                                {
                                    LSport_ScreenLeagueDto lg = new LSport_ScreenLeagueDto()
                                    {
                                        ShowLeague = false,
                                        LeagueName = game.LocationName + " - " + game.LeagueName,   ///game.LeagueName + " - " + game.LocationName,
                                        LeagueId = game.LeagueId,
                                        Games = new List<LSportGameDto>()
                                    };

                                    lg.Games.Add(game);
                                    EventSportLines.Where(x => x != null && x.SportId == game.SportId).FirstOrDefault()
                                        .Leagues.Add(lg);
                                }
                            }
                            else
                            {
                                LSport_ScreenLeagueDto lg = new LSport_ScreenLeagueDto()
                                {
                                    ShowLeague = false,
                                    LeagueName = game.LocationName + " - " + game.LeagueName, //game.LeagueName + " - " + game.LocationName,
                                    LeagueId = game.LeagueId,
                                    Games = new List<LSportGameDto>()
                                };
                                lg.Games.Add(game);
                                LSport_ScreenSportsDto sl = new LSport_ScreenSportsDto()
                                {
                                    ShowSport = false,
                                    SportName = game.SportName,
                                    SportId = game.SportId,
                                    Leagues = new List<LSport_ScreenLeagueDto>()
                                };
                                sl.Leagues.Add(lg);
                                EventSportLines.Add(sl);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
            return EventSportLines;
        }

        public TotalWagersDTO GetTotalWagersByGame(int prmIdGame)
        {
            TotalWagersDTO? ProfileLimitsResp = new TotalWagersDTO();
            try
            {
                string dgsConnString = "Data Source=192.168.11.29;Initial Catalog=DGSDATA;Persist Security Info=True;User ID=Payments;Password=p@yM3nts2701;TrustServerCertificate=True";
                string sql = "exec Game_GetWagersCount @prmIdGame";
                var values = new { prmIdGame };
                using var connection = new SqlConnection(dgsConnString);
                ProfileLimitsResp = connection.Query<TotalWagersDTO>(sql, values).FirstOrDefault();
            }
            catch (Exception ex)
            {
                _ = ex.Message;
            }
            return ProfileLimitsResp;
        }

        public GameDGS GetInfoPrematchDGS(int FixtureId)
        {
            GameDGS oGame = new GameDGS();
            try
            {
                string dgsConnString = "Data Source=192.168.11.29;Initial Catalog=DGSDATA;Persist Security Info=True;User ID=Payments;Password=p@yM3nts2701;TrustServerCertificate=True";
                string sql = "exec GetPrematchIdGameByFixtureId @FixtureId";
                var values = new { FixtureId };
                using var connection = new SqlConnection(dgsConnString);
                oGame = connection.Query<GameDGS>(sql, values).FirstOrDefault();
            }
            catch (Exception ex)
            {
                _ = ex.Message;
            }
            return oGame;
        }

        public List<LSport_ScreenSportsDto> GetSignalFixtures()
        {
            List<LSport_ScreenSportsDto> EventSportLines = new List<LSport_ScreenSportsDto>();
            try
            {
                //  var GameList = GetAllEventsByOur(hours); //obtemenos todos los fixtures de las ultimas 3 horas
                var GameList = GetSignalEvents();



                foreach (var game in GameList)
                {

                    // if (game.TotalLines > 0)
                    // {
                    var teams = GetParticipants(game.FixtureId);
                    if (teams != null)
                    {
                        game.HomeTeam = teams.Where(x => x.Position == 1).FirstOrDefault()?.Name;
                        game.HomeTeamId = teams.Where(x => x.Position == 1).FirstOrDefault() != null
                            ? Convert.ToInt32(teams.Where(x => x.Position == 1).FirstOrDefault().ParticipantId)
                            : 0;
                        game.VisitorTeam = teams.Where(x => x.Position == 2).FirstOrDefault()?.Name;
                        game.VisitorTeamId = teams.Where(x => x.Position == 2).FirstOrDefault() != null
                            ? Convert.ToInt32(teams.Where(x => x.Position == 2).FirstOrDefault().ParticipantId)
                            : 0;
                    }

                    //check if this sports already exists in EventSportLines
                    if (EventSportLines.Where(x => x != null && x.SportId == game.SportId).Any())
                    {
                        //yes, sport exists
                        if (EventSportLines.Where(x => x != null && x.SportId == game.SportId).FirstOrDefault()
                            .Leagues.Where(f => f.LeagueId == game.LeagueId).Any())
                        {
                            EventSportLines.Where(x => x != null && x.SportId == game.SportId).FirstOrDefault()
                                .Leagues.Where(f => f.LeagueId == game.LeagueId).FirstOrDefault().Games.Add(game);
                        }
                        else
                        {
                            LSport_ScreenLeagueDto lg = new LSport_ScreenLeagueDto()
                            {
                                ShowLeague = false,
                                LeagueName = game.LeagueName + " - " + game.LocationName,
                                LeagueId = game.LeagueId,
                                Games = new List<LSportGameDto>()
                            };

                            lg.Games.Add(game);
                            EventSportLines.Where(x => x != null && x.SportId == game.SportId).FirstOrDefault()
                                .Leagues.Add(lg);
                        }

                    }
                    else
                    {
                        LSport_ScreenLeagueDto lg = new LSport_ScreenLeagueDto()
                        {
                            ShowLeague = false,
                            LeagueName = game.LeagueName + " - " + game.LocationName,
                            LeagueId = game.LeagueId,
                            Games = new List<LSportGameDto>()
                        };
                        lg.Games.Add(game);




                        LSport_ScreenSportsDto sl = new LSport_ScreenSportsDto()
                        {
                            ShowSport = false,
                            SportName = game.SportName,
                            SportId = game.SportId,
                            Leagues = new List<LSport_ScreenLeagueDto>()

                        };

                        sl.Leagues.Add(lg);


                        EventSportLines.Add(sl);
                    }
                    //}
                }
            }
            catch (Exception ex)
            {

            }
            return EventSportLines;
        }

        public List<LSport_ScreenSportsDto> GetPartialGamesAndLines(int FixtureId)
        {
            List<LSport_ScreenSportsDto> EventSportLines = new List<LSport_ScreenSportsDto>();
            try
            {
                var GameValues = GetEventByFixture(FixtureId); //obtemenos todos los fixtures de las ultimas 3 horas
                                                               //var sports = GetSports();
                                                               //var leagues = GetLeagues();
                                                               //var locations = GetLocations();

                if (GameValues != null)
                {

                    var teams = GetParticipants(GameValues.FixtureId);
                    if (teams != null)
                    {
                        GameValues.HomeTeam = teams.Where(x => x.Position == 1).FirstOrDefault()?.Name;
                        GameValues.HomeTeamId = teams.Where(x => x.Position == 1).FirstOrDefault() != null ? Convert.ToInt32(teams.Where(x => x.Position == 1).FirstOrDefault().ParticipantId) : 0;
                        GameValues.VisitorTeam = teams.Where(x => x.Position == 2).FirstOrDefault()?.Name;
                        GameValues.VisitorTeamId = teams.Where(x => x.Position == 2).FirstOrDefault() != null ? Convert.ToInt32(teams.Where(x => x.Position == 2).FirstOrDefault().ParticipantId) : 0;
                    }

                    //check if this sports already exists in EventSportLines
                    if (EventSportLines.Where(x => x != null && x.SportId == GameValues.SportId).Any())
                    {
                        //yes, sport exists
                        //  if (EventSportLines.Where(x => x != null && x.SportId == GameValues.SportId).FirstOrDefault().Locations.Where(y => y != null && y.LocationId == GameValues.LocationId).Any())
                        // {
                        //yes, location exists
                        if (EventSportLines.Where(x => x != null && x.SportId == GameValues.SportId).FirstOrDefault().Leagues.Where(f => f.LeagueId == GameValues.LeagueId).Any())
                        {
                            EventSportLines.Where(x => x != null && x.SportId == GameValues.SportId).FirstOrDefault().Leagues.Where(f => f.LeagueId == GameValues.LeagueId).FirstOrDefault().Games.Add(GameValues);
                        }
                        else
                        {
                            LSport_ScreenLeagueDto lg = new LSport_ScreenLeagueDto()
                            {
                                ShowLeague = false,
                                LeagueName = GameValues.LeagueName,
                                LeagueId = GameValues.LeagueId,
                                Games = new List<LSportGameDto>()
                            };

                            lg.Games.Add(GameValues);
                            EventSportLines.Where(x => x != null && x.SportId == GameValues.SportId).FirstOrDefault().Leagues.Add(lg);
                        }
                        //}
                        //else
                        //{
                        //    //location does not exist

                        //    // var locationSel = locations.Where(x => x.loc == score.LocationId).FirstOrDefault();
                        //    // var leagueSel = leagues.Where(x => x.LeagueID == score.LeagueId).FirstOrDefault();

                        //    LSport_ScreenLocationDto lc = new LSport_ScreenLocationDto()
                        //    {
                        //        ShowLocation = false,
                        //        LocationName = GameValues.LocationName,
                        //        LocationId = GameValues.LocationId,
                        //        Leagues = new List<LSport_ScreenLeagueDto>()
                        //    };

                        //    LSport_ScreenLeagueDto lg = new LSport_ScreenLeagueDto()
                        //    {
                        //        ShowLeague = false,
                        //        LeagueName = GameValues.LeagueName,
                        //        LeagueId = GameValues.LeagueId,
                        //        Games = new List<LSportGameDto>()
                        //    };

                        //    lg.Games.Add(GameValues);

                        //    lc.Leagues.Add(lg);

                        //    EventSportLines.Where(x => x != null && x.SportId == GameValues.SportId).FirstOrDefault().Locations.Add(lc);

                        //}//end add location

                    }
                    else
                    {

                        LSport_ScreenLeagueDto lg = new LSport_ScreenLeagueDto()
                        {
                            ShowLeague = false,
                            LeagueName = GameValues.LeagueName,
                            LeagueId = GameValues.LeagueId,
                            Games = new List<LSportGameDto>()
                        };
                        lg.Games.Add(GameValues);




                        LSport_ScreenSportsDto sl = new LSport_ScreenSportsDto()
                        {
                            ShowSport = false,
                            SportName = GameValues.SportName,
                            SportId = GameValues.SportId,
                            Leagues = new List<LSport_ScreenLeagueDto>()

                        };

                        sl.Leagues.Add(lg);


                        EventSportLines.Add(sl);
                    }
                }
            }
            catch (Exception ex)
            {

            }
            return EventSportLines;
        }


        public List<CompletePropMarket> GetEventsMarketsLines(int FixtureId, int SportId)
        {
            List<CompletePropMarket> resp = new List<CompletePropMarket>();
            try
            {
                using (var connection = new SqlConnection(connString))
                {
                    var procedure = "[sp_MGL_GetEventsMarketsLinesPreMatch]";
                    var values = new
                    {
                        FixtureId = FixtureId,
                        SportId = SportId,
                    };
                    resp = connection.Query<CompletePropMarket>(procedure, values, commandType: CommandType.StoredProcedure).ToList();
                }
            }
            catch (Exception ex)
            {
                resp = null;
            }
            return resp;
        }

        //este metodo obtiene las lineas por ID
        //public CompletePropMarket GetPropById(long LineId)
        //{
        //    CompletePropMarket resp = new CompletePropMarket();
        //    try
        //    {
        //        using (var connection = new SqlConnection(connString))
        //        {
        //            var procedure = "[sp_MGL_GetEventsMarketsLineById]";
        //            var values = new
        //            {
        //                IdLine = LineId
        //            };
        //            resp = connection.Query<CompletePropMarket>(procedure, values, commandType: CommandType.StoredProcedure).FirstOrDefault();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        resp = null;
        //    }
        //    return resp;
        //}

        public string FixBaseLineStr(string baseline)
        {
            string newbaseline = baseline;
            if (!String.IsNullOrEmpty(baseline))
            {
                string originalString = baseline;

                int index = originalString.IndexOf("#");

                if (index != -1)
                {
                    string result = originalString.Substring(0, index);
                    newbaseline = result;
                }
                else
                {
                    // Console.WriteLine("El caracter '#' no se encontró en la cadena.");
                }
            }

            return newbaseline;
        }

        //este metodo toma el objeto de markeys y lineas y lo convierten en el objeto de jerarquia Market -> hijos Lineas
        private List<LSport_EventPropMarketDto> ConvertToScreenProps(List<CompletePropMarket> originalList, int FixtureId)
        {
            //List<int> UnderOverProps = new List<int>(new int[] { 21, 28, 45, 46, 47, 77, 153, 155, 220, 221, 337, 354, 335, 430, 466, 916, 920, 927, 928, 1194, 1196, 1197, 1198, 1199, 1200, 1202, 1203, 1218, 1783, 2400, 2541 });
            //List<int> UnderOverProps = new List<int>(new int[] { 1196 });
            var teams = GetParticipants(FixtureId);
            List<LSport_EventPropMarketDto> resp = new List<LSport_EventPropMarketDto>();
            try
            {
                foreach (var line in originalList)
                {
                    var market = resp.Where(x => x.MarketID == line.MarketId).FirstOrDefault();

                    if (market != null) //si existe el market.. agregamos el prop
                    {
                        market.Props.Add(new LSport_EventPropDto
                        {
                            MarketId = line.MarketId,
                            IdL1 = line.Id.ToString(),
                            FixtureId = line.FixtureId,
                            Line1 = line.Line,
                            MarketName = line.MarketName,
                            Odds1 = int.Parse(line.PriceUS),
                            Name = line.Name == "1" ? teams.Where(x => x.Position == 1).FirstOrDefault()?.Name : line.Name == "2" ? teams.Where(x => x.Position == 2).FirstOrDefault()?.Name : line.Name == "X" ? "DRAW" : line.Name.ToUpper(),
                            IsSelected = false,
                            BaseLine = FixBaseLineStr(line.BaseLine),
                            OriginalName = line.Name,
                            Price = Convert.ToDecimal(line.Price)
                        });
                    }
                    else
                    {
                        var a = new LSport_EventPropMarketDto
                        {
                            MainLine = line.MainLine,
                            MarketID = line.MarketId,
                            MarketName = line.MarketName,
                            IsTnt = line.IsTnt,
                            IsPlayerProp = line.IsPlayerProp,
                            IsGameProp = line.IsGameProp,
                            IsMain = line.IsMain,
                            AllowMarketParlay = line.AllowMarketParlay,
                            Props = new List<LSport_EventPropDto>()
                        };
                        a.Props.Add(new LSport_EventPropDto
                        {
                            MarketId = line.MarketId,
                            IdL1 = line.Id.ToString(),
                            FixtureId = line.FixtureId,
                            Line1 = line.Line,
                            MarketName = line.MarketName,
                            Odds1 = int.Parse(line.PriceUS),
                            Name = line.Name == "1" ? teams.Where(x => x.Position == 1).FirstOrDefault()?.Name : line.Name == "2" ? teams.Where(x => x.Position == 2).FirstOrDefault()?.Name : line.Name == "X" ? "DRAW" : line.Name.ToUpper(),
                            IsSelected = false,
                            BaseLine = FixBaseLineStr(line.BaseLine),
                            OriginalName = line.Name,
                            Price = Convert.ToDecimal(line.Price)

                        });
                        resp.Add(a);
                    }
                }
            }
            catch (Exception ex)
            {
                resp = null;
            }
            return resp;
        }

        //private List<LSport_EventPropDto> BuildOverUnderMarkets(CompletePropMarket prop, List<LSport_EventPropDto> currentProps)
        //{

        //    if (currentProps != null)
        //    {

        //        var line = currentProps.Where(x => x.Line1 == prop.BaseLine).FirstOrDefault();

        //        if (line != null)
        //        {
        //            if (prop.Name == "Over")
        //            {
        //                line.Odds1 = int.Parse(prop.PriceUS);
        //                line.Id1 = prop.Id.ToString();
        //            }
        //            else
        //            {
        //                line.Odds2 = int.Parse(prop.PriceUS);
        //                line.Id2 = prop.Id.ToString();
        //            }
        //        }
        //        else
        //        {
        //            currentProps.Add(new LSport_EventPropDto
        //            {
        //                Id1 = prop.Name == "Over" ? prop.Id : 0,
        //                Id2 = prop.Name == "Under" ? prop.Id : 0,
        //                Line1 = prop.BaseLine,
        //                Odds1 = prop.Name == "Over" ? int.Parse(prop.PriceUS) : null,
        //                Odds2 = prop.Name == "Under" ? int.Parse(prop.PriceUS) : null,
        //            });
        //        }

        //    }
        //    else
        //    {
        //        currentProps = new List<LSport_EventPropDto>();

        //        var line = new LSport_EventPropDto();
        //        line.Line1 = prop.BaseLine;
        //        if (prop.Name == "Over")
        //        {
        //            line.Odds1 = int.Parse(prop.PriceUS);
        //            line.Id1 = prop.Id;
        //        }
        //        else
        //        {
        //            line.Odds2 = int.Parse(prop.PriceUS);
        //            line.Id2 = prop.Id;
        //        }

        //        currentProps.Add(line);
        //    }


        //    return currentProps;
        //}

        //este metodo convierte la linea que viene da db al objeto que vamos a retornar en el FE

        private LSport_EventPropDto TranslateProp(CompletePropMarket prop)
        {
            LSport_EventPropDto resp = new LSport_EventPropDto();
            //decimal n;
            // bool isNumeric = decimal.TryParse(prop.Line, out n);
            //if (isNumeric)
            //{
            //    resp.Line1 = n;
            //}
            resp.Line1 = prop.BaseLine;
            resp.Odds1 = int.Parse(prop.PriceUS);
            //  int nameNumber;
            //  bool isNameNumeric = decimal.TryParse(prop.Name, out n);
            //if(isNameNumeric) { 
            //}
            resp.MarketValue = prop.Name;
            resp.IdL1 = prop.Id.ToString();

            return resp;
        }

        public List<ParticipantDto> GetParticipants(int FixtureId)
        {
            List<ParticipantDto> Participants = new List<ParticipantDto>();
            try
            {
                using var connection = new SqlConnection(connString);
                Participants = connection.Query<ParticipantDto>("SELECT [ParticipantId] ,[Name] ,[Position] ,[Rot] FROM [Mover].[dbo].[tb_MGL_Participant] WHERE FIXTUREID = " + FixtureId).ToList();
            }
            catch (Exception ex)
            {

            }
            return Participants;
        }

        public List<OpenBetsDTO> GetOpenBets(int IdPlayer)
        {
            List<OpenBetsDTO> resp = new List<OpenBetsDTO>();
            string sql = "exec sp_MGL_GetOpenBets @IdPlayer";
            var values = new { IdPlayer };
            try
            {
                using var connection = new SqlConnection(connString);
                resp = connection.Query<OpenBetsDTO>(sql, values).ToList();
            }
            catch (Exception ex)
            {
                _ = ex.Message;
            }
            return resp;
        }

        public List<HistoryBetsDTO> GetHistoryBets(HistoryReq req)
        {
            List<HistoryBetsDTO> resp = new List<HistoryBetsDTO>();
            string sql = "exec sp_MGL_GetHistoryBets @IdPlayer, @InitDate, @EndDate";
            var values = new { req.IdPlayer, req.InitDate, req.EndDate };
            try
            {
                using var connection = new SqlConnection(connString);
                resp = connection.Query<HistoryBetsDTO>(sql, values).ToList();
            }
            catch (Exception ex)
            {
                _ = ex.Message;
            }
            return resp;
        }
    }//end class


}//end namespace
