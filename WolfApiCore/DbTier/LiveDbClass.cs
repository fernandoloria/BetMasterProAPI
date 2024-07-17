
using Dapper;
using Microsoft.Data.SqlClient;
using System.Collections.Immutable;
using System.Data;
using WolfApiCore.Models;
using static WolfApiCore.Models.AdminModels;

namespace WolfApiCore.DbTier
{
    public class LiveDbClass
    {
        private readonly string connString;

        public LiveDbClass(string ConnString)
        {
            connString = ConnString;
        }

        //private List<LSportGameDto> GetAllEventsByHour(int hours)
        //{
        //    List<LSportGameDto> resultList = new List<LSportGameDto>();
        //    string sql = "exec sp_MGL_GetEventsByHour @hours";
        //    var values = new { hours = hours };

        //    try
        //    {
        //        using var connection = new SqlConnection(connString);
        //        resultList = connection.Query<LSportGameDto>(sql, values).ToList();


        //        foreach (var game in resultList)
        //        {
        //            //get markets/lines


        //            game.PropMarkets = ConvertToScreenProps(GetEventsMarketsLines(game.FixtureId), game.FixtureId);


        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        //here todo log
        //        string value = ex.Message;
        //    }

        //    return resultList;
        //}//end GetAllScores

        private List<LSportGameDto> GetAllActiveEvents()
        {
            List<LSportGameDto> games = new List<LSportGameDto>();
            string sql = "exec [sp_MGL_GetActiveEvents]";
            // var values = new { StatusId = StatusId };

            try
            {
                using var connection = new SqlConnection(connString);
                games = connection.Query<LSportGameDto>(sql/*, values*/).ToList();

                foreach (var game in games)
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

            return games;
        }//end GetAllScores

        public List<LSportGameDto> GetSignalEvents()
        {
            var signalEvents = new List<LSportGameDto>();    
            try
            {
                using var connection = new SqlConnection(connString);
                signalEvents = connection.Query<LSportGameDto>("exec [sp_MGL_GetSignalEvents]"/*, new { StatusId = StatusId }*/).ToList();


                foreach (var game in signalEvents)
                {
                    //get markets/lines
                    game.PropMarkets = ConvertToScreenProps(
                                            GetEventsMarketsLines(game.FixtureId, game.SportId), 
                                            game.FixtureId
                                       );

                }

            }
            catch (Exception ex)
            {
                //here todo log
                string value = ex.Message;
            }

            return signalEvents;
        }//end GetAllScores


        
        public List<LSportGameDto> GetSignalEventsV2()
        {
            var signalEvents = new List<GameMarketAndLinesDTO>();
            var gamesDTO = new List<LSportGameDto>();

            try
            {
                using (var connection = new SqlConnection(connString))
                {
                    //trae todos los events con los market y lineas aplanados en una sola consulta...
                    signalEvents = connection.Query<GameMarketAndLinesDTO>("exec [sp_MGL_GetSignalEventsV2]").ToList();
                }

                var fixtures = signalEvents.GroupBy(e => e.FixtureId);

                foreach (var fixtureGroup in fixtures)
                { 
                    var fixture = fixtureGroup.First();

                    var game = new LSportGameDto
                    {
                        FixtureId = fixture.FixtureId,
                        StatusId = fixture.StatusId,
                        Status_Description = fixture.Status_Description,
                        Status_DescriptionCss = fixture.Status_DescriptionCss,
                        CurrentPeriod = fixture.CurrentPeriod,
                        PremiumGame = fixture.PremiumGame,
                        BetCount = fixture.BetCount,
                        GameTime = fixture.GameTime,
                        PeriodDesc = fixture.PeriodDesc,
                        GameStatus = fixture.GameStatus,
                        VisitorScore = fixture.VisitorScore,
                        HomeScore = fixture.HomeScore,
                        ScoreCreatedDateTime = fixture.ScoreCreatedDateTime,
                        SportId = fixture.SportId,
                        SportName = fixture.SportName,
                        LeagueId = fixture.LeagueId,
                        LeagueName = fixture.LeagueName,
                        EventFixtureDateTime = fixture.EventFixtureDateTime,
                        VisitorRotation = fixture.VisitorRotation,
                        HomeRotation = fixture.HomeRotation,
                        VisitorTeamId = fixture.VisitorTeamId,
                        HomeTeamId = fixture.HomeTeamId,
                        EventCreatedDateTime = fixture.EventCreatedDateTime,
                        IdGame = fixture.IdGame,
                        VisitorTeam = fixture.VisitorTeam,
                        HomeTeam = fixture.HomeTeam,
                        LocationId = fixture.LocationId,
                        LocationName = fixture.LocationName,
                        GameId = fixture.GameId,
                        ShowLines = fixture.ShowLines,
                        TotalLines = fixture.TotalLines,
                        IsTournament = fixture.IsTournament,
                        PropMarkets = new List<LSport_EventPropMarketDto>()
                    };                    

                    var markets = fixtureGroup.GroupBy(m => m.MarketId);
                    foreach (var marketGroup in markets)
                    {
                        var market = marketGroup.First();

                        var gameMarket = new LSport_EventPropMarketDto
                        {
                            MainLine = market.MainLine,
                            MarketID = market.MarketId,
                            MarketName = market.MarketName!,
                            IsTnt = market.IsTnt,
                            IsPlayerProp = market.IsPlayerProp,
                            IsGameProp = market.IsGameProp,
                            IsMain = market.IsMain,
                            AllowMarketParlay = market.AllowMarketParlay,
                            Explanation = market.Explanation,
                            Props = new List<LSport_EventPropDto>()
                        };                        

                        foreach (var linea in marketGroup)
                        {
                            gameMarket.Props.Add(new LSport_EventPropDto
                            {
                                MarketId = linea.MarketId,
                                IdL1 = linea.Id.ToString(),
                                FixtureId = linea.FixtureId,
                                Line1 = linea.Line,
                                MarketName = linea.MarketName,
                                Odds1 = int.Parse(linea.PriceUS!),
                                Name = linea.Name!.ToUpper(),
                                IsSelected = false,
                                BaseLine = FixBaseLineStr(linea.BaseLine),
                                OriginalName = linea.Name,
                                Price = Convert.ToDecimal(linea.Price)
                            });
                        }
                        game.PropMarkets.Add(gameMarket);
                    }
                    gamesDTO.Add(game);
                }
            }
            catch (Exception ex)
            {
                //here todo log
                string value = ex.Message;
            }

            return gamesDTO;
        }


        public List<LSportGameDto> GetSignalEventsV3()
        // usar multi-mapping de Dapper...
        {
            var signalEvents = new List<GameMarketAndLinesDTO>();
            var gamesDTO = new List<LSportGameDto>();

            try
            {
                using (var connection = new SqlConnection(connString))
                {
                    connection.Open();

                    var fixturesDic = new Dictionary<int, LSportGameDto>();

                    var results = connection.Query<LSportGameDto, LSport_EventPropMarketDto, CompletePropMarket, LSportGameDto>(
                       @"EXEC sp_MGL_GetSignalEventsV2",
                       (game, market, prop) =>
                       {

                           LSportGameDto gameEntry;

                           if (!fixturesDic.TryGetValue(game.FixtureId, out gameEntry!))
                           {
                               gameEntry = game;
                               gameEntry.PropMarkets = new List<LSport_EventPropMarketDto>();
                               fixturesDic.Add(gameEntry.FixtureId, gameEntry);
                               gamesDTO.Add(gameEntry);
                           }                          


                           var currentMarket = gameEntry.PropMarkets!.FirstOrDefault(m => m.MarketID == market.MarketID);
                           if (currentMarket == null)
                           {
                               currentMarket = new LSport_EventPropMarketDto
                               {
                                   MainLine = market.MainLine,
                                   MarketID = market.MarketID,
                                   MarketName = market.MarketName,
                                   IsTnt = market.IsTnt,
                                   IsPlayerProp = market.IsPlayerProp,
                                   IsGameProp = market.IsGameProp,
                                   IsMain = market.IsMain,
                                   AllowMarketParlay = market.AllowMarketParlay,
                                   Explanation = market.Explanation,
                                   Props = new List<LSport_EventPropDto>()
                               };

                               gameEntry.PropMarkets.Add(currentMarket);
                           }                          

                           currentMarket.Props.Add(new LSport_EventPropDto
                           {
                               MarketId = prop.MarketId,
                               IdL1 = prop.Id.ToString(),
                               FixtureId = prop.FixtureId,
                               Line1 = prop.Line,
                               MarketName = market.MarketName,
                               Odds1 = int.Parse(prop.PriceUS!),
                               Name = prop.Name!.ToUpper(),
                               IsSelected = false,
                               BaseLine = FixBaseLineStr(prop.BaseLine!),
                               OriginalName = prop.Name,
                               Price = Convert.ToDecimal(prop.Price)
                           });

                           return gameEntry; // Devuelve el objeto principal (LSportGameDto)
                       },
                       splitOn: "MarketId, Id" // Especifica cómo dividir los resultados por cada objeto
                   );

                   //gamesDTO = results.ToList();
                }
            }
            catch (Exception ex)
            {
                //here todo log
                string value = ex.Message;
            }

            return gamesDTO;
        }



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

                var gameList = GetAllActiveEvents();

                if (idPlayer > 0)
                {
                    var oPlayerHierarchy = oLiveDbWager.GetPlayerHierarchy(idPlayer);
                    List<GetSportsAndLeaguesHiddenReq> request = new List<GetSportsAndLeaguesHiddenReq>();
                    request.Add(new GetSportsAndLeaguesHiddenReq()
                    {
                        AgentId = oPlayerHierarchy.SubAgentId,
                        PlayerId = oPlayerHierarchy.PlayerId
                    });

                    var blockedSportLeagues = oLiveAdmin.GetSportsAndLeaguesHidden(request).ToList();
                    
                    foreach (var b in blockedSportLeagues)
                    {
                        var sportId = b.SportId;
                        var leagueId = b.LeagueId;

                        if (sportId != null && leagueId == null)
                        {
                            // remover deporte
                            gameList = gameList.Where(g => g.SportId != sportId).ToList();
                        }
                        else
                            // remover la liga
                            gameList = gameList.Where(g => g.LeagueId != leagueId).ToList();
                    }
                    
                    
                    foreach (var game in gameList)
                    {
                        var oGameDGS = GetInfoPrematchDGS(game.FixtureId);
                        if (oGameDGS != null) {
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
                                var homeTeam = teams.Where(x => x.Position == 1).FirstOrDefault();                                
                                if (homeTeam != null) 
                                {
                                    game.HomeTeam = homeTeam?.Name;
                                    game.HomeTeamId = Convert.ToInt32(homeTeam?.ParticipantId);
                                    game.HomeRotation = Convert.ToInt32(homeTeam?.Rot);
                                }

                                var visitorTeam = teams.Where(x => x.Position == 2).FirstOrDefault();                                
                                if (visitorTeam != null)
                                {
                                    game.VisitorTeam = visitorTeam?.Name;
                                    game.VisitorTeamId = Convert.ToInt32(visitorTeam?.ParticipantId);
                                    game.VisitorRotation = Convert.ToInt32(visitorTeam?.Rot);
                                }
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
                                        LeagueName = $"{ (HasLocationNameException(game.LocationName) ? game.LeagueName : $"{game.LocationName} - {game.LeagueName}") }",   ///game.LeagueName + " - " + game.LocationName,
                                        LeagueId = game.LeagueId,
                                        IsTournament = game.IsTournament,
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
                                    LeagueName = $"{ (HasLocationNameException(game.LocationName) ? game.LeagueName : $"{game.LocationName} - {game.LeagueName}") }", //game.LeagueName + " - " + game.LocationName,
                                    LeagueId = game.LeagueId,
                                    IsTournament = game.IsTournament,
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

        public GameDGS GetInfoPrematchDGS(int FixtureId) {
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
            List<LSport_ScreenSportsDto> sportList = new List<LSport_ScreenSportsDto>();
            try
            {
                //  var GameList = GetAllEventsByOur(hours); //obtemenos todos los fixtures de las ultimas 3 horas
                var gameList = GetSignalEventsV2();//GetSignalEvents();

                foreach (var game in gameList)
                {

                    // if (game.TotalLines > 0)
                    // {

                    /* Esto ya se hace internamente en el metodo GetSingalEvents
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
                    }*/


                    //Buscar el deporte
                    var sport = sportList.Where(s => s.SportId == game.SportId).FirstOrDefault();

                    //Si no existe
                    if (sport == null)
                    {                        
                        sport = new LSport_ScreenSportsDto()
                        {
                            ShowSport = false,
                            SportName = game.SportName,
                            SportId = game.SportId,
                            Leagues = new List<LSport_ScreenLeagueDto>()

                        };

                        //Agregar el deporte
                        sportList.Add(sport); 
                    }                                        

                    //Buscar la liga
                    var league = sport!.Leagues.Where(l => l.LeagueId == game.LeagueId).FirstOrDefault();

                    //Si no existe
                    if (league == null) 
                    {                        
                        league = new LSport_ScreenLeagueDto()
                        {
                            ShowLeague = false,
                            LeagueName = $"{(HasLocationNameException(game.LocationName) ? game.LeagueName : $"{game.LocationName} - {game.LeagueName}")}",
                            LeagueId = game.LeagueId,
                            Games = new List<LSportGameDto>()
                        };

                        //Agregar la liga
                        sport.Leagues.Add(league);
                    }

                    //Agregar el juego
                    league.Games.Add(game);                    
                }
            }
            catch (Exception ex)
            {

            }
            return sportList;
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

        //public LSport_EventValuesDto GetEventValues(long fixtureId)
        //{
        //    LSport_EventValuesDto eventValues = new LSport_EventValuesDto();
        //    try
        //    {
        //        using var connection = new SqlConnection(connString);
        //        var values = connection.Query<LSport_EventValuesDto>(@"SELECT [FixtureID]
        //                                                                      ,[MarketID]
        //                                                                      ,[VisitorSpread]
        //                                                                      ,[VisitorSpreadOdds]
        //                                                                      ,[HomeSpread]
        //                                                                      ,[HomeSpreadOdds]
        //                                                                      ,[VisitorML]
        //                                                                      ,[HomeML]
        //                                                                      ,[Total]
        //                                                                      ,[TotalOver]
        //                                                                      ,[TotalUnder]
        //                                                                      ,[LastUpdate]
        //                                                                  FROM [DGSData].[dbo].[DBA_LSportsEventValues]
        //                                                                  where FixtureID = " + fixtureId).FirstOrDefault();

        //        if (values != null)
        //            eventValues = values;

        //        eventValues.ShowInScreen = false;
        //    }
        //    catch (Exception ex)
        //    {

        //    }
        //    return ConvertStrValues(eventValues);
        //}

        //public List<LSport_EventPropMarketDto> GetEventProps(long fixtureId)
        //{
        //    List<LSport_EventPropMarketDto> eventValues = new List<LSport_EventPropMarketDto>();
        //    try
        //    {
        //        using var connection = new SqlConnection(connString);
        //        var propList = connection.Query<LSport_EventPropDto>(@"SELECT  [FixtureID]
        //                                                                      ,PROP.MarketId
        //                                                                      ,[Line1]
        //                                                                      ,[Line2]
        //                                                                      ,[Line3]
        //                                                                      ,[Odds1]
        //                                                                      ,[Odds2]
        //                                                                      ,[Odds3]
        //                                                                      ,[LineStatus]
        //                                                                      ,[LastUpdateDateTime], 
        //                                                                   MARK.MarketName,
        //                                                                   Mark.MarketType
        //                                                              FROM [DGSData].[dbo].[DBA_LSportsEvent_GameProps] PROP
        //                                                              JOIN [DGSData].[dbo].[DBA_LSportsMarket] MARK ON PROP.MARKETID = MARK.MARKETID
        //                                                              WHERE FIXTUREID =" + fixtureId + " AND LINESTATUS = 1").ToList();


        //        foreach (var itemProp in propList)
        //        {
        //            var market = eventValues.Where(x => x.MarketID == itemProp.MarketId).FirstOrDefault();

        //            if (market == null)//no existe
        //            {
        //                LSport_EventPropMarketDto m = new LSport_EventPropMarketDto
        //                {
        //                    MarketID = itemProp.MarketId,
        //                    MarketName = itemProp.MarketName,
        //                    Props = new List<LSport_EventPropDto>()
        //                };
        //                m.Props.Add(itemProp);
        //                eventValues.Add(m);
        //            }
        //            else
        //            {
        //                market.Props.Add(itemProp);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {

        //    }
        //    return eventValues; //ConvertPropsStrValues(eventValues);
        //}

        //public LSport_EventValuesDto ConvertStrValues(LSport_EventValuesDto line)
        //{
        //    string spread = "";
        //    string ml = "";
        //    string total = "";

        //    try
        //    {
        //        #region spread

        //        if (line.HomeSpread != null)
        //        {
        //            if (line.HomeSpread > 0)
        //                spread = "+" + line.HomeSpread;
        //            else
        //                spread = line.HomeSpread.ToString();
        //        }
        //        if (line.HomeSpreadOdds != null)
        //        {
        //            if (line.HomeSpreadOdds > 0)
        //                spread += " +" + line.HomeSpreadOdds;
        //            else
        //                spread += " " + line.HomeSpreadOdds.ToString();
        //        }

        //        line.HomeSpreadStr = spread;
        //        spread = "";

        //        if (line.VisitorSpread != null)
        //        {
        //            if (line.VisitorSpread > 0)
        //                spread = "+" + line.VisitorSpread;
        //            else
        //                spread = line.VisitorSpread.ToString();
        //        }
        //        if (line.VisitorSpreadOdds != null)
        //        {
        //            if (line.VisitorSpreadOdds > 0)
        //                spread += " +" + line.VisitorSpreadOdds;
        //            else
        //                spread += " " + line.VisitorSpreadOdds.ToString();
        //        }
        //        line.VisitorSpreadStr = spread;

        //        #endregion

        //        #region ML
        //        if (line.HomeML != null)
        //        {
        //            if (line.HomeML > 0)
        //                ml = "+" + line.HomeML;
        //            else
        //                ml = line.HomeML.ToString();
        //        }

        //        line.HomeMLStr = ml;
        //        ml = "";

        //        if (line.VisitorML != null)
        //        {
        //            if (line.VisitorML > 0)
        //                ml = "+" + line.VisitorML;
        //            else
        //                ml = line.VisitorML.ToString();
        //        }

        //        line.VisitorMLStr = ml;

        //        #endregion

        //        #region Total
        //        if (line.Total != null)
        //        {
        //            /*   if (line.Total > 0)
        //                   total = line.Total.ToString();
        //               else*/
        //            total = line.Total.ToString();
        //        }
        //        if (line.TotalOver != null)
        //        {
        //            if (line.TotalOver > 0)
        //                total += " +" + line.TotalOver;
        //            else
        //                total += " " + line.TotalOver.ToString();
        //        }

        //        line.VisitorTotalStr = total;
        //        total = "";

        //        if (line.Total != null)
        //        {
        //            /*  if (line.Total > 0)
        //                  total = "+" + line.Total;
        //              else*/
        //            total = line.Total.ToString();
        //        }
        //        if (line.TotalUnder != null)
        //        {
        //            if (line.TotalUnder > 0)
        //                total += " +" + line.TotalUnder;
        //            else
        //                total += " " + line.TotalUnder.ToString();
        //        }
        //        line.HomeTotalStr = total;

        //        #endregion

        //    }
        //    catch (Exception ex)
        //    {

        //    }

        //    return line;
        //}

        //private List<LSport_SportsDto> GetSports()
        //{
        //    List<LSport_SportsDto> Sports = new List<LSport_SportsDto>();
        //    try
        //    {
        //        using var connection = new SqlConnection(connString);
        //        Sports = connection.Query<LSport_SportsDto>("SELECT [SportID], [SportName] FROM [DGSData].[dbo].[DBA_LSports_Sport]").ToList();
        //    }
        //    catch (Exception ex)
        //    {

        //    }
        //    return Sports;
        //}

        //private List<LSport_Leagues> GetLeagues()
        //{
        //    List<LSport_Leagues> Leagues = new List<LSport_Leagues>();
        //    try
        //    {
        //        using var connection = new SqlConnection(connString);
        //        Leagues = connection.Query<LSport_Leagues>("SELECT [LeagueID], [SportID],[LeagueName], [LocationID]  FROM [DGSData].[dbo].[DBA_LSports_League]").ToList();
        //    }
        //    catch (Exception ex)
        //    {

        //    }
        //    return Leagues;
        //}

        //private List<LSport_LocationDto> GetLocations()
        //{
        //    List<LSport_LocationDto> Locations = new List<LSport_LocationDto>();
        //    try
        //    {
        //        using var connection = new SqlConnection(connString);
        //        Locations = connection.Query<LSport_LocationDto>("SELECT  [LocationID], [LocationName]  FROM [DGSData].[dbo].[DBA_LSports_Location]").ToList();
        //    }
        //    catch (Exception ex)
        //    {

        //    }
        //    return Locations;
        //}


        //este metodo obtiene las lineas de un juego en particular ( markets y lineas )

        public List<CompletePropMarket> GetEventsMarketsLines(int FixtureId, int SportId)
        {
            List<CompletePropMarket> resp = new List<CompletePropMarket>();
            try
            {
                using (var connection = new SqlConnection(connString))
                {
                    var procedure = "[sp_MGL_GetEventsMarketsLines]";
                    var values = new
                    {
                        FixtureId = FixtureId,
                        SportId = SportId
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
            if (!String.IsNullOrEmpty(baseline))
            {               
                int index = baseline.IndexOf("#");

                if (index >= 0)                
                    baseline = baseline.Substring(0, index);                
            }

            return baseline;
        }

        //este metodo toma el objeto de markets y lineas y lo convierten en el objeto de jerarquia Market -> hijos Lineas
        private List<LSport_EventPropMarketDto> ConvertToScreenProps(List<CompletePropMarket> gameLines, int FixtureId)
        {
            //List<int> UnderOverProps = new List<int>(new int[] { 21, 28, 45, 46, 47, 77, 153, 155, 220, 221, 337, 354, 335, 430, 466, 916, 920, 927, 928, 1194, 1196, 1197, 1198, 1199, 1200, 1202, 1203, 1218, 1783, 2400, 2541 });
            //List<int> UnderOverProps = new List<int>(new int[] { 1196 });
            var participants = GetParticipants(FixtureId);
            var resp = new List<LSport_EventPropMarketDto>();
            try
            {
                var newMarket = false;
                foreach (var line in gameLines)
                {
                    var market = resp.Where(x => x.MarketID == line.MarketId).FirstOrDefault();

                    newMarket = market == null;

                    if (newMarket) //si no esxise el market, lo creamos
                    {                        
                        market = new LSport_EventPropMarketDto
                        {
                            MainLine = line.MainLine,
                            MarketID = line.MarketId,
                            MarketName = line.MarketName,
                            IsTnt = line.IsTnt,
                            IsPlayerProp = line.IsPlayerProp,
                            IsGameProp = line.IsGameProp,
                            IsMain = line.IsMain,
                            AllowMarketParlay = line.AllowMarketParlay,
                            Explanation = line.Explanation,
                            Props = new List<LSport_EventPropDto>()
                        };                        
                    }

                    market!.Props.Add(new LSport_EventPropDto
                    {
                        MarketId = line.MarketId,
                        IdL1 = line.Id.ToString(),
                        FixtureId = line.FixtureId,
                        Line1 = line.Line,
                        MarketName = line.MarketName,
                        Odds1 = int.Parse(line.PriceUS),
                        Name = line.Name == "1" ? participants.Where(x => x.Position == 1).FirstOrDefault()?.Name 
                             : line.Name == "2" ? participants.Where(x => x.Position == 2).FirstOrDefault()?.Name 
                             : line.Name == "X" ? "DRAW" 
                             : line.Name!.ToUpper(),
                        IsSelected = false,
                        BaseLine = FixBaseLineStr(line.BaseLine),
                        OriginalName = line.Name,
                        Price = Convert.ToDecimal(line.Price)
                    });

                    if(newMarket)
                        resp.Add(market);
                }
                /*

                foreach (var prop in originalList)
                {
                    if (prop.Line == null && prop.BaseLine == null)
                    {
                        if ((resp.Where(x => x.MarketID == prop.MarketId).FirstOrDefault() == null))
                        {
                            //creamos el marquet
                            var newMarket = new LSport_EventPropMarketDto
                            {
                                MarketID = prop.MarketId,
                                MarketName = prop.MarketName,
                                MainLine = prop.MainLine,
                                Props = new List<LSport_EventPropDto>()
                            };

                            newMarket.Props.Add(new LSport_EventPropDto
                            {
                                MarketId = prop.MarketId,
                                MarketName = prop.MarketName,
                                Name = prop.Name == "1" ? teams.Where(x => x.Position == 1).FirstOrDefault()?.Name : prop.Name == "2" ? teams.Where(x => x.Position == 2).FirstOrDefault()?.Name : prop.Name == "X" ? "DRAW" : prop.Name.ToUpper(),
                                OriginalName = prop.Name,
                                Odds1 = int.Parse(prop.PriceUS),
                                Price = Convert.ToDecimal(prop.PriceUS),
                                IdL1 = prop.Id.ToString(),
                                IsSelected = false
                              //  MainLine = prop.MainLine
                            });

                            resp.Add(newMarket);
                        }
                        else
                        {
                            resp.Where(x => x.MarketID == prop.MarketId).FirstOrDefault().Props.Add(new LSport_EventPropDto
                            {
                                MarketId = prop.MarketId,
                                MarketName = prop.MarketName,
                                Name = prop.Name == "1" ? teams.Where(x => x.Position == 1).FirstOrDefault()?.Name : prop.Name == "2" ? teams.Where(x => x.Position == 2).FirstOrDefault()?.Name : prop.Name == "X" ? "DRAW" : prop.Name.ToUpper(),
                                OriginalName = prop.Name,
                                Odds1 = int.Parse(prop.PriceUS),
                                Price = Convert.ToDecimal(prop.PriceUS),
                                IdL1 = prop.Id.ToString(),
                                IsSelected = false
                             //   MainLine = prop.MainLine
                            });
                        }
                    }
                    else if (prop.Line != null && prop.BaseLine != null)
                    {
                        if ((resp.Where(x => x.MarketID == prop.MarketId).FirstOrDefault() == null))
                        {
                            //creamos el marquet
                            var newMarket = new LSport_EventPropMarketDto
                            {
                                MarketID = prop.MarketId,
                                MarketName = prop.MarketName,
                                MainLine = prop.MainLine,
                                Props = new List<LSport_EventPropDto>()
                            };

                            newMarket.Props.Add(new LSport_EventPropDto
                            {
                                MarketId = prop.MarketId,
                                MarketName = prop.MarketName,
                                Line1 = prop.Line,
                                BaseLine = prop.BaseLine,
                                Name = prop.Name == "1" ? teams.Where(x => x.Position == 1).FirstOrDefault()?.Name : prop.Name == "2" ? teams.Where(x => x.Position == 2).FirstOrDefault()?.Name : prop.Name == "X" ? "DRAW" : prop.Name.ToUpper(),
                                Odds1 = int.Parse(prop.PriceUS),
                                Price = Convert.ToDecimal(prop.PriceUS),
                                IdL1 = prop.Id.ToString(),
                                OriginalName = prop.Name,
                                IsSelected = false
                                // MainLine = prop.MainLine
                            }); 

                            resp.Add(newMarket);
                        }
                        else
                        {
                            resp.Where(x => x.MarketID == prop.MarketId).FirstOrDefault().Props.Add(new LSport_EventPropDto
                            {
                                MarketId = prop.MarketId,
                                MarketName = prop.MarketName,
                                Name = prop.Name == "1" ? teams.Where(x => x.Position == 1).FirstOrDefault()?.Name : prop.Name == "2" ? teams.Where(x => x.Position == 2).FirstOrDefault()?.Name : prop.Name == "X" ? "DRAW" : prop.Name.ToUpper(),
                                Line1 = prop.Line,
                                BaseLine = prop.BaseLine,
                                Odds1 = int.Parse(prop.PriceUS),
                                Price = Convert.ToDecimal(prop.PriceUS),
                                IdL1 = prop.Id.ToString(),
                                OriginalName = prop.Name,
                                IsSelected = false
                               // MainLine = prop.MainLine
                            });
                        }
                    }
                }

                */

                /*
                foreach (var prop in originalList)
                {
                    //es un market overUnder y no hay datos...  primer prop... 
                    if ((resp.Where(x => x.MarketID == prop.MarketId).FirstOrDefault() == null))
                    {
                        //creamos el marquet
                        var newMarket = new LSport_EventPropMarketDto
                        {

                            MarketID = prop.MarketId,
                            MarketName = prop.MarketName,
                            Props = new List<LSport_EventPropDto>()
                        };
                        //revisamos si es un market overUnder
                        if (UnderOverProps.Where(x => x == prop.MarketId).FirstOrDefault() != 0)  //si es OverUnder
                        {
                            newMarket.Props = BuildOverUnderMarkets(prop, null);
                            resp.Add(newMarket);
                        }
                        else //no es un overUnder
                        {
                            newMarket.Props.Add(TranslateProp(prop));
                            resp.Add(newMarket);
                        }
                    }
                    else //ya existe el market.. entonces solo agregamos el prop
                    {
                        //revisamos si es un market overUnder
                        if (UnderOverProps.Where(x => x == prop.MarketId).FirstOrDefault() != 0)
                        {
                            resp.Where(x => x.MarketID == prop.MarketId).FirstOrDefault().Props = BuildOverUnderMarkets(prop, resp.Where(x => x.MarketID == prop.MarketId).FirstOrDefault().Props);
                        }
                        else
                        {
                            resp.Where(x => x.MarketID == prop.MarketId).FirstOrDefault().Props.Add(TranslateProp(prop));
                        }

                    }
                }
                */
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
            string sql = "exec sp_Live_GetParticipants @FixtureId";
            var values = new { FixtureId };
            try
            {
                using var connection = new SqlConnection(connString);
                Participants = connection.Query<ParticipantDto>(sql, values).ToList();
            }
            catch (Exception ex)
            {
                _ = ex.Message;
            }
            return Participants;
        }

        public List<OpenBetsDTO> GetOpenBets(int IdPlayer) {
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

        public void WriteSignalRUpdaterIP(string ipAddress)
        {
            try
            {
                string sql = "exec sp_MGL_SignalRLog @ip";
                var values = new { ip = ipAddress };
                using var connection = new SqlConnection(connString);
                connection.Execute(sql, values);
            }
            catch (Exception ex)
            {
                _ = ex.Message;
            }
            //return oGame;
        }

        private bool HasLocationNameException(string locationName)
        {
            string[] locationNameExceptions = { "United States" };        

            return locationName.Contains(locationNameExceptions[0]);
        }


    }//end class

    public class OpenBetsDTO {
        public int IdPlayer { get; set; }
        public int IdLiveWager { get; set; }
        public int IdWagerType { get; set; }
        public int DgsIdWager { get; set; }
        public DateTime PlacedDateTime { get; set; }
        public decimal RiskAmount { get; set; }
        public decimal WinAmount { get; set; }
        public string Description { get; set; }
        public string CompleteDescription { get; set; }
        public int FixtureId { get; set; }
        public decimal Odds { get; set; }
        public decimal Price { get; set; }
        public string PickTeam { get; set; }
        public int MarketId { get; set; }
        public int Result { get; set; }
        public string? SportName { get; set; }
        public string? Home { get; set; }
        public string? Visitor { get; set; }
        public string? Market { get; set; }
        public string? Player { get; set; }
        public string? Line { get; set; }
        public string? LeagueName { get; set; }
    }

    public class HistoryBetsDTO
    {
        public int IdPlayer { get; set; }
        public int IdLiveWager { get; set; }
        public int IdWagerType { get; set; }
        public int DgsIdWager { get; set; }
        public DateTime PlacedDateTime { get; set; }
        public decimal RiskAmount { get; set; }
        public decimal WinAmount { get; set; }
        public string Description { get; set; }
        public string CompleteDescription { get; set; }
        public int FixtureId { get; set; }
        public decimal Odds { get; set; }
        public decimal Price { get; set; }
        public string PickTeam { get; set; }
        public int MarketId { get; set; }
        public int Result { get; set; }

    }

    public class HistoryReq {
        public int IdPlayer { get; set; }
        public DateTime InitDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class ParticipantDto
    {
        public int ParticipantId { get; set; }
        public string Name { get; set; }
        public int Position { get; set; }
        public string Rot { get; set; }      

    }

    public class TotalWagersDTO
    {
        public int WagersCount { get; set; }
    }

    public class GameDGS
    {
        public int IdGame { get; set; }
    }
}//end namespace
