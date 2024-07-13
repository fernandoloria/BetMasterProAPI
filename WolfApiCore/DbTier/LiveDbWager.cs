using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using WolfApiCore.LSportApi;
using WolfApiCore.Models;
using static WolfApiCore.Models.AdminModels;

namespace WolfApiCore.DbTier
{
    public class LiveDbWager
    {
        private readonly string DgsConnString = "Data Source=192.168.11.29;Initial Catalog=DGSDATA;Persist Security Info=True;User ID=Payments;Password=p@yM3nts2701;TrustServerCertificate=True";
        private readonly string MoverConnString = "Data Source=192.168.11.29;Initial Catalog=mover;Persist Security Info=True;User ID=live;Password=d_Ez*gIb8v7NogU;TrustServerCertificate=True";

        //private readonly AppConfig _appConfig = new AppConfig();

        //solo para spread
        private static readonly List<int> SpreadMarkets = new List<int>{
            3, 13, 53, 61, 64, 65, 66, 67, 68, 95, 201, 250, 281, 283, 307, 342, 386, 407, 408, 433, 447, 
            448, 449, 450, 467, 468, 526, 555, 757, 866, 935, 958, 1083, 1149, 1150, 1151, 1152, 1153, 1215, 
            1227, 1228, 1231, 1232, 1240, 1241, 1248, 1270, 1318, 1319, 1320, 1321, 1322, 1367, 1368, 1369, 
            1370, 1371, 1372, 1373, 1374, 1375, 1376, 1380, 1381, 1382, 1383, 1384, 1385, 1386, 1390, 1391, 
            1394, 1439, 1541, 1558, 1579, 1580, 1581, 1609, 1610, 1620, 1622, 1691, 1692, 1693, 1694, 1835, 
            1877, 1878, 1967, 2035, 2040, 2052, 2057, 2063, 2095, 2191, 2259, 2286, 2342, 2389, 2391, 2393, 
            2395, 2397, 2408, 2409, 2410, 2411, 2675, 2732
        };

        private static readonly string[] leagueNameExceptions = { "E-Sports" };


        public LSport_BetSlipObj ValidateSelectionsForWagers(LSport_BetSlipObj betslip)
        {
            var validForParlay = true;

            // if (Betslip.IdPlayer != 300563)
            //     return Betslip;

            var cl = new List<CheckListLines>();

            //validamos todos
            foreach (var fixture in betslip.Events)
            {
                foreach (var selection in fixture.Selections)
                {
                    cl.Add(new CheckListLines { 
                        FixtureId = fixture.FixtureId, 
                        MarketId = selection.MarketId, 
                        BetId = Convert.ToInt64(selection.IdL1) 
                    });
                }
            }


            // ojo en lsportSnapShot retorna un checkList modificado (por el line closed de linea... if any!)
            var lsportSnapshot = GetLSportsBetsInfo(cl);
            foreach (var fixture in betslip.Events)
            {
                foreach (var item in fixture.Selections)
                {
                    var snapshotItem = lsportSnapshot.FirstOrDefault(s => s.BetId == Convert.ToInt64(item.IdL1));
                    if (null != snapshotItem && item.BsRiskAmount != null)
                    {
                        var dbProp = ValidateBetSlipItem(item, snapshotItem.BetInfo, betslip.AcceptLineChange, fixture.FixtureId, betslip.IdPlayer, item.MarketId);

                        if (dbProp.StatusForWager != 9 && dbProp.StatusForWager != 10)
                            validForParlay = false;
                    }
                }
            }


            //INSERT STRAIGHTS
            foreach (var fixture in betslip.Events)
            {
                foreach (var item in fixture.Selections)
                {
                    if (item.BsWinAmount != null)
                    {
                        if (item.BsWinAmount > 0)
                        {
                            if ((int)item.BsRiskAmount >= 0 && (item.StatusForWager is 10 or 9))
                            {
                                CreateStraightWagerModel straightWagerModel = new CreateStraightWagerModel
                                {
                                    PickSelected = item,
                                    FixtureId = fixture.FixtureId,
                                    IdPlayer = betslip.IdPlayer,
                                    HomeTeam = fixture.HomeTeam,
                                    VisitorTeam = fixture.VisitorTeam,
                                    SportName = fixture.SportName,
                                    IsMobile = betslip.IsMobile,
                                    LeagueName = fixture.LeagueName,
                                    IsTournament = fixture.IsTournament
                                };

                                var betResult = CreateStraightWager(straightWagerModel);
                                item.StatusForWager = betResult.StatusForWager;
                                item.BsBetResult = betResult.BsBetResult;
                                item.BsTicketNumber = betResult.BsTicketNumber;

                                if (item.BsBetResult == -1 )
                                {
                                    item.BsMessage = "Wager Rejected";
                                }

                            }
                        }
                    }
                }
            }

            //Parlay ??
            var minRiskAmount = (decimal)5;
            var maxRiskAmount = (decimal)1000;
            var maxWinAmount = (decimal)2000;
            var minPriceAmount = (decimal)-1000;
            var maxPriceAmount = (decimal)1000;
            var totAmtPerGame = (decimal)500;
            var playerLimitsParlay = GetPlayerLimitsParlay(betslip.IdPlayer);
            var agentLimitsParlay = GetAgentLimitsParlay(betslip.IdPlayer);

            if (playerLimitsParlay != null)
            {
                minRiskAmount = playerLimitsParlay.MinWager;
                maxRiskAmount = playerLimitsParlay.MaxWager;
                maxWinAmount = playerLimitsParlay.MaxPayout;
                minPriceAmount = playerLimitsParlay.MinPrice;
                maxPriceAmount = playerLimitsParlay.MaxPrice;
                totAmtPerGame = playerLimitsParlay.TotAmtGame;
            }
            else if (agentLimitsParlay != null)
            {
                minRiskAmount = agentLimitsParlay.MinWager;
                maxRiskAmount = agentLimitsParlay.MaxWager;
                maxWinAmount = agentLimitsParlay.MaxPayout;
                minPriceAmount = agentLimitsParlay.MinPrice;
                maxPriceAmount = agentLimitsParlay.MaxPrice;
                totAmtPerGame = agentLimitsParlay.TotAmtGame;
            }

            if (validForParlay && betslip.ParlayRiskAmount > 0 && betslip.ParlayRiskAmount >= minRiskAmount)
            {
                foreach (var fixture in betslip.Events)
                {
                    foreach (var selection in fixture.Selections)
                    {
                        var snapshotItem = lsportSnapshot.FirstOrDefault(s => s.BetId == Convert.ToInt64(selection.IdL1));
                        if (null != snapshotItem && selection.BsRiskAmount == null)
                        {
                            if (snapshotItem != null && snapshotItem.BetInfo!.Status! == 1 /*Line Open*/)
                            {
                                if (int.Parse(snapshotItem.BetInfo!.PriceUS) == selection.Odds1 &&
                                    snapshotItem.BetInfo!.Line == selection.Line1)
                                {
                                    selection.StatusForWager = 10;  //ready for wager
                                    selection.BaseLine = snapshotItem.BetInfo!.BaseLine;
                                }
                                else
                                {
                                    selection.StatusForWager = 10;
                                    selection.Odds1 = int.Parse(snapshotItem.BetInfo!.PriceUS);
                                    selection.Price = Convert.ToDecimal(snapshotItem.BetInfo!.Price);
                                    selection.Line1 = snapshotItem.BetInfo!.Line;
                                    selection.BaseLine = snapshotItem.BetInfo!.BaseLine;
                                    selection.BsBetResult = -51;
                                    selection.BsMessage = "Line Change Detected.";
                                    selection.IdL2 = (null != snapshotItem.BetInfo!.AlternateId) ? snapshotItem.BetInfo!.AlternateId.ToString() : "";
                                }
                            }
                            else
                            {
                                //linea cerrada
                                selection.BsBetResult = -52;
                                selection.StatusForWager = 5;
                                selection.BsMessage = "Line closed";
                            }


                        }

                        var totalAmountValue = GetTotalValuePerGame(betslip.IdPlayer, fixture.FixtureId, selection.MarketId) + betslip.ParlayRiskAmount;
                        if (totalAmountValue > totAmtPerGame)
                        {
                            betslip.ParlayBetResult = -50;
                            betslip.ParlayMessage = $"{fixture.HomeTeam} vs {fixture.VisitorTeam} exceeds Max Risk per game. (Max = {totAmtPerGame:F0})";
                            continue;
                        }

                        if (selection.Odds1 < minPriceAmount)
                        {
                            betslip.ParlayBetResult = -50;
                            betslip.ParlayMessage = $"Price for {selection.Name} exceeds Min Price. (Min = {minPriceAmount:F0})";
                        }
                        else if (selection.Odds1 > maxPriceAmount)
                        {
                            betslip.ParlayBetResult = -50;
                            betslip.ParlayMessage = $"Price for {selection.Name} exceeds Max Price. (Max = {maxPriceAmount:F0})";
                        }
                    }
                }


                if (betslip.ParlayRiskAmount > maxRiskAmount)
                {
                    betslip.ParlayBetResult = -50;
                    betslip.ParlayMessage = $"Parlay exceeds Max Risk amount. (Max ={maxRiskAmount:F0})";
                }
                else if (betslip.ParlayWinAmount > maxWinAmount)
                {
                    betslip.ParlayBetResult = -50;
                    betslip.ParlayMessage = $"Parlay exceeds Max Win amount. (Max = {maxWinAmount:F0})";
                }
                else if(betslip.ParlayBetResult == 0)
                {
                    var obj = CreateParlayWager(betslip);
                    betslip.ParlayBetResult = obj.ParlayBetResult;
                    betslip.ParlayBetTicket = obj.ParlayBetTicket;
                }
            }
            else if (validForParlay)
            {
                betslip.ParlayBetResult = -50;
                betslip.ParlayMessage = "";
            }
            else
            {
                betslip.ParlayBetResult = -60;
                betslip.ParlayMessage = "";
            }

            return betslip;
        }

        public decimal ParlayCalculateWin(LSport_BetSlipObj Betslip)
        {
            float factor = GeParlayFactor(Betslip);
            return Convert.ToDecimal((decimal)factor * Betslip.ParlayRiskAmount) - Betslip.ParlayRiskAmount;
        }

        public RespPlayerLastBet GetLastBetHours(int PlayerId)
        {
            RespPlayerLastBet lastBet = new RespPlayerLastBet();
            try
            {
                using var connection = new SqlConnection(MoverConnString);
                connection.Open();

                lastBet = connection.QueryFirstOrDefault<RespPlayerLastBet>(
                    "sp_MGL_GetLastBetHours",
                    new { IdPlayer = PlayerId },
                    commandType: System.Data.CommandType.StoredProcedure);

                if (lastBet == null)
                {
                    lastBet = new RespPlayerLastBet
                    {
                        idPlayer = PlayerId,
                        PlacedDateTime = DateTime.Now, 
                        HoursSinceLastBet = 100 
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return lastBet;
        }


        public float GeParlayFactor(LSport_BetSlipObj betslip)
        {
            float factor = 1;

            try
            {
                foreach (var fixture in betslip.Events)
                {
                    foreach (var selection in fixture.Selections)
                    {
                        float originalOdd = (float)selection.Odds1;
                        if (originalOdd > 0)
                            factor *= ((originalOdd / 100) + 1);
                        else
                            factor *= ((100 / (-1 * originalOdd)) + 1);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return factor;
        }

        public decimal StraightCalculateWin(int odd, decimal risk)   
        {
            decimal win = 0;

            try
            {
                if (odd < 0) //negativo -110
                {
                    win = -1 * ((100 * risk) / odd);  // R: 110 to Win: 100
                }
                else // positivo +110
                {
                    win = (odd * risk) / 100;  // R:100 to Win: 110
                }
            }
            catch (Exception)
            {

                return 0;
            }

            return win;
        }

        private decimal GetBetAmount(LSport_EventPropDto betslipItem)
        {
            return Math.Min(betslipItem.BsRiskAmount.Value, betslipItem.BsWinAmount.Value);
        }

        public LSport_EventPropDto ValidateBetSlipItem(LSport_EventPropDto betslipItem, Bet snapshotItem,  bool acceptLineChanged, int fixtureId, int idplayer, int idMarket)
        {
            try
            {
                var betAmount = GetBetAmount(betslipItem);
                var limits = GetPlayerLimits(idplayer);

                var minBetAmount = (decimal)10;//500   **estos son los oldsvalues**
                var maxBetAmount = (decimal)1000;//100
                var maxPayout = (decimal)2000;//100
                var minPriceAmount = (decimal)-1000;
                var maxPriceAmount = (decimal)1000;
                var totAmtPerGame = (decimal)500;

                var playerLimitsStraight = GetPlayerLimitsStraight(idplayer, fixtureId);
                var agentLimitsStraight = GetAgentLimitsStraight(idplayer, fixtureId);
                var totalAmountValue = GetTotalValuePerGame(idplayer, fixtureId, idMarket) + betAmount;

                if (playerLimitsStraight != null)
                {
                    minBetAmount = playerLimitsStraight.MinWager;
                    maxBetAmount = playerLimitsStraight.MaxWager;
                    maxPayout = playerLimitsStraight.MaxPayout;
                    minPriceAmount = playerLimitsStraight.MinPrice;
                    maxPriceAmount = playerLimitsStraight.MaxPrice;
                    totAmtPerGame = playerLimitsStraight.TotAmtGame;
                }
                else if (agentLimitsStraight != null)
                {
                    minBetAmount = agentLimitsStraight.MinWager;
                    maxBetAmount = agentLimitsStraight.MaxWager;
                    maxPayout = agentLimitsStraight.MaxPayout;
                    minPriceAmount = agentLimitsStraight.MinPrice;
                    maxPriceAmount = agentLimitsStraight.MaxPrice;
                    totAmtPerGame = agentLimitsStraight.TotAmtGame;
                }

                if (!GetPlayerInfo(idplayer).Access)
                {
                    betslipItem.StatusForWager = 5;//Player  no tiene acceso a VegasLives
                    betslipItem.BsBetResult = -50;
                    betslipItem.BsMessage = "Access denied, contact your Agent.";
                }
                else 
                {
                    if (snapshotItem != null && snapshotItem.Status == 1 /*Line Open*/)
                    {
                        if (int.Parse(snapshotItem.PriceUS) == betslipItem.Odds1 &&
                            snapshotItem.Line == betslipItem.Line1)
                        {
                            betslipItem.StatusForWager = 10;  //ready for wager
                            betslipItem.BaseLine = snapshotItem.BaseLine;                            
                        }
                        else
                        {
                            //Si la linea cambio pero el usuario aceptó el cambio de linea...
                            /*if (acceptLineChanged)
                            {
                                betslipItem.StatusForWager = 9;  
                                betslipItem.Odds1 = int.Parse(snapshotItem.PriceUS);
                                betslipItem.Price = Convert.ToDecimal(snapshotItem.Price);
                                betslipItem.Line1 = snapshotItem.Line;
                                betslipItem.BaseLine = snapshotItem.BaseLine;
                            }
                            //linea cambio y el usuario no ha aceptado el cambio de linea
                            else*/
                            {                            
                                betslipItem.StatusForWager = 5; 
                                betslipItem.Odds1 = int.Parse(snapshotItem.PriceUS);
                                betslipItem.Price = Convert.ToDecimal(snapshotItem.Price);
                                betslipItem.Line1 = snapshotItem.Line;
                                betslipItem.BaseLine = snapshotItem.BaseLine;
                                betslipItem.BsBetResult = -51;
                                betslipItem.BsMessage = "Line Change Detected.";                                
                                betslipItem.IdL2 = (null != snapshotItem.AlternateId) ? snapshotItem.AlternateId.ToString() : ""; 
                            }
                        }
                    }
                    else
                    {
                        //linea cerrada
                        betslipItem.StatusForWager = 5;
                        betslipItem.BsBetResult = -52;
                        betslipItem.BsMessage = "Line closed";
                    }

                    if ((betslipItem.StatusForWager is 10 or 9) && betslipItem.BsRiskAmount > 0)
                    {
                        if (betslipItem.Odds1 < minPriceAmount )
                        {
                            betslipItem.StatusForWager = 5;
                            betslipItem.BsBetResult = -50;
                            betslipItem.BsMessage = $"Ticket exceeds Min Price. (Min {minPriceAmount:F0})";
                        }                            
                        else if (betslipItem.Odds1 > maxPriceAmount)
                        {
                            betslipItem.StatusForWager = 5;
                            betslipItem.BsBetResult = -50;
                            betslipItem.BsMessage = $"Ticket exceeds Max Price. (Max {maxPriceAmount:F0})";
                        }
                        else if (totalAmountValue > totAmtPerGame)
                        {
                            betslipItem.StatusForWager = 5;
                            betslipItem.BsBetResult = -50;
                            betslipItem.BsMessage = $"Ticket exceeds Max Total Amount per game. (Max {totAmtPerGame:F0})";
                        }

                        //Remarks: 03/24/2024 Donovan requested to Check Min a
                        // bet amount tiene q estar entre minBetAmount y maxBetAmount
                        else if (betAmount < minBetAmount)
                        {
                            betslipItem.StatusForWager = 5; 
                            betslipItem.BsBetResult = -50;
                            betslipItem.BsMessage = $"Less than Min Wager. (Min {minBetAmount:F0})";
                        }
                        
                        else if (betAmount > maxBetAmount)
                        {
                            betslipItem.StatusForWager = 5;
                            betslipItem.BsBetResult = -50;
                            betslipItem.BsMessage = $"Exceeded Max Wager. (Max {maxBetAmount:F0})";
                        }               
                    
                        // Comparar el WinAmount con el MaxPayot
                        else if (betslipItem.BsWinAmount > maxPayout)
                        {
                            betslipItem.StatusForWager = 5;
                            betslipItem.BsBetResult = -50;
                            betslipItem.BsMessage = $"Exceeded Max Payout. (Max {maxPayout:F0})";
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                //linea cerrada
                betslipItem.StatusForWager = 5;
                betslipItem.BsBetResult = -50;
                betslipItem.BsMessage = "Error trying to push the bet";
            }
            return betslipItem;
        }

        private PlayerLimitsHierarchyParlay GetPlayerLimitsParlay(int PlayerId)
        {
            var oPlayerHierarchy = GetPlayerHierarchy(PlayerId);
            LiveDbClass oLiveClass = new LiveDbClass(MoverConnString);
            LiveAdminDbClass oAdminClass = new LiveAdminDbClass();
            PlayerLimitsHierarchyParlay resp = new PlayerLimitsHierarchyParlay
            {
                PlayerId = PlayerId,
                LeagueId = null,
                SportId = -1,
                IsLeagueLimit = false,
                IsSportLimit = false,
                MinWager = 0,
                MaxWager = 0,
                MaxPayout = 0,
                MinPrice = 0,   
                MaxPrice = 0,
                TotAmtGame = 0,
            };
            try
            {
                //var GameInfo = oLiveClass.GetEventByFixture(FixtureId);
                ProfileLimitsByPlayerReq oReq = new ProfileLimitsByPlayerReq
                {
                    IdWagerType = 2,
                    PlayerId = PlayerId,
                    SportId = -1,
                    LeagueId = null,
                    FixtureId = null
                };
                // Validate first the Player - Sport //
                var oLimitsSport = oAdminClass.GetProfileLimitsByIdPlayer(oReq);
                if (oLimitsSport != null)
                {
                    resp.IsSportLimit = true;
                    resp.SportId = -1;
                    resp.MinWager = oLimitsSport.MinWager;
                    resp.MaxWager = oLimitsSport.MaxWager;
                    resp.MaxPayout = oLimitsSport.MaxPayout;


                    resp. MinPrice = oLimitsSport.MinPrice;
                    resp.MaxPrice = oLimitsSport.MaxPrice;
                    resp.TotAmtGame = oLimitsSport.TotAmtGame;
                }
                else
                {
                    // Validate the Player - League //
                    resp = null;
                    //oReq.LeagueId = GameInfo.LeagueId;
                    //var oLimitsLeague = oAdminClass.GetProfileLimitsByIdPlayer(oReq);
                    //if (oLimitsLeague != null)
                    //{
                    //    resp.IsSportLimit = false;
                    //    resp.IsLeagueLimit = true;
                    //    resp.SportId = GameInfo.SportId;
                    //    resp.LeagueId = GameInfo.LeagueId;
                    //    resp.MinWager = oLimitsLeague.MinWager;
                    //    resp.MaxWager = oLimitsLeague.MaxWager;
                    //    resp.MaxPayout = oLimitsLeague.MaxPayout;
                    //}
                    //else
                    //{
                    //    resp = null;
                    //}
                }
            }
            catch (Exception ex)
            {
                //throw ex;
                resp = null;
            }
            return resp;
        }

        private PlayerLimitsHierarchyStraight GetPlayerLimitsStraight(int PlayerId, int FixtureId)
        {
            var oPlayerHierarchy = GetPlayerHierarchy(PlayerId);
            LiveDbClass oLiveClass = new LiveDbClass(MoverConnString);
            LiveAdminDbClass oAdminClass = new LiveAdminDbClass();
            PlayerLimitsHierarchyStraight resp = new PlayerLimitsHierarchyStraight
            {
                PlayerId = PlayerId,
                SportId = null,
                LeagueId = null,
                IsLeagueLimit = false,
                IsSportLimit = false,
                MinWager = 0,
                MaxWager = 0,
                MaxPayout = 0,
                MinPrice = 0,
                MaxPrice = 0,
                TotAmtGame = 0,
            };
            try
            {
                //GetProfileLimitsByIdPlayer(ProfileLimitsByPlayerReq req)
                var GameInfo = oLiveClass.GetEventByFixture(FixtureId);
                ProfileLimitsByPlayerReq oReq = new ProfileLimitsByPlayerReq
                {
                    IdWagerType = 1,
                    PlayerId = PlayerId,
                    SportId = GameInfo.SportId,
                    LeagueId = null,
                    FixtureId = null
                };

                // Validate first the Player - Sport //
                var oLimitsSport = oAdminClass.GetProfileLimitsByIdPlayer(oReq);

                if (oLimitsSport != null)
                {
                    resp.IsSportLimit = true;
                    resp.SportId = GameInfo.SportId;
                    resp.MinWager = oLimitsSport.MinWager;
                    resp.MaxWager = oLimitsSport.MaxWager;
                    resp.MaxPayout = oLimitsSport.MaxPayout;

                    resp.MinPrice = oLimitsSport.MinPrice;
                    resp.MaxPrice = oLimitsSport.MaxPrice;
                    resp.TotAmtGame = oLimitsSport.TotAmtGame;
                }
                else
                {
                    // Validate the Player - League //
                    oReq.LeagueId = GameInfo.LeagueId;
                    var oLimitsLeague = oAdminClass.GetProfileLimitsByIdPlayer(oReq);
                    if (oLimitsLeague != null)
                    {
                        resp.IsSportLimit = false;
                        resp.IsLeagueLimit = true;
                        resp.SportId = GameInfo.SportId;
                        resp.LeagueId = GameInfo.LeagueId;
                        resp.MinWager = oLimitsLeague.MinWager;
                        resp.MaxWager = oLimitsLeague.MaxWager;
                        resp.MaxPayout = oLimitsLeague.MaxPayout;

                        resp.MinPrice = oLimitsLeague.MinPrice;
                        resp.MaxPrice = oLimitsLeague.MaxPrice;
                        resp.TotAmtGame = oLimitsLeague.TotAmtGame;
                    }
                    else
                    {
                        resp = null;
                    }
                }
            }
            catch (Exception ex)
            {
                resp = null;
            }
            return resp;
        }



        private int GetTotalValuePerGame(int PlayerId, int FixtureId, int Marketid)
        {
            var limits = 0;
            try
            {
                using var connection = new SqlConnection(MoverConnString);
                limits = connection.Query<int>(@"SELECT [dbo].[fn_TotalAmountPlayedByGameMarket] (@PlayerId,@FixtureId , @Marketid)", new { PlayerId, FixtureId, Marketid }).FirstOrDefault();
            }
            catch (Exception ex)
            {
                _ = ex.Message;
            }
            return limits;
        }


        private AgentLimitsHierarchyStraight GetAgentLimitsStraight(int PlayerId, int FixtureId)
        {
            var oPlayerHierarchy = GetPlayerHierarchy(PlayerId);
            LiveDbClass oLiveClass = new LiveDbClass(MoverConnString);
            LiveAdminDbClass oAdminClass = new LiveAdminDbClass();
            AgentLimitsHierarchyStraight resp = new AgentLimitsHierarchyStraight
            {
                AgentId = oPlayerHierarchy.SubAgentId,
                MasterAgentId = oPlayerHierarchy.MasterAgentId,
                LeagueId = null,
                SportId = null,
                IsLeagueLimit = false,
                IsSportLimit = false,
                MinWager = 0,
                MaxWager = 0,
                MaxPayout = 0,
                MinPrice = 0,
                MaxPrice = 0,
                TotAmtGame = 0,
            };
            try
            {
                //First Validate the SubAgent Limits by Sport & League //
                var GameInfo = oLiveClass.GetEventByFixture(FixtureId);
                GetProfileLimitsReq oReq = new GetProfileLimitsReq
                {
                    IdWagerType = 1,
                    AgentId = oPlayerHierarchy.SubAgentId,
                    SportId = GameInfo.SportId,
                    LeagueId = null,
                    FixtureId = null
                };
                // Validate first the Player - Sport //
                var oLimitsSport = oAdminClass.GetProfileLimitsByIdAgent(oReq);
                if (oLimitsSport != null)
                {
                    resp.IsSportLimit = true;
                    resp.SportId = GameInfo.SportId;
                    resp.MinWager = oLimitsSport.MinWager;
                    resp.MaxWager = oLimitsSport.MaxWager;
                    resp.MaxPayout = oLimitsSport.MaxPayout;

                    resp.MinPrice = oLimitsSport.MinPrice;
                    resp.MaxPrice = oLimitsSport.MaxPrice;
                    resp.TotAmtGame = oLimitsSport.TotAmtGame;
                }
                else
                {
                    // Validate the Player - League //
                    oReq.LeagueId = GameInfo.LeagueId;
                    var oLimitsLeague = oAdminClass.GetProfileLimitsByIdAgent(oReq);
                    if (oLimitsLeague != null)
                    {
                        resp.IsSportLimit = false;
                        resp.IsLeagueLimit = true;
                        resp.SportId = GameInfo.SportId;
                        resp.LeagueId = GameInfo.LeagueId;
                        resp.MinWager = oLimitsLeague.MinWager;
                        resp.MaxWager = oLimitsLeague.MaxWager;
                        resp.MaxPayout = oLimitsLeague.MaxPayout;

                        resp.MinPrice = oLimitsLeague.MinPrice;
                        resp.MaxPrice = oLimitsLeague.MaxPrice;
                        resp.TotAmtGame = oLimitsLeague.TotAmtGame;
                    }
                }

                //Validate the master agent limits //
                if (!resp.IsSportLimit && !resp.IsLeagueLimit)
                {
                    GetProfileLimitsReq oReqMaster = new GetProfileLimitsReq
                    {
                        IdWagerType = 1,
                        AgentId = oPlayerHierarchy.MasterAgentId,
                        SportId = GameInfo.SportId,
                        LeagueId = null,
                        FixtureId = null
                    };
                    // Validate first the Player - Sport //
                    var oLimitsSportMaster = oAdminClass.GetProfileLimitsByIdAgent(oReqMaster);
                    if (oLimitsSportMaster != null)
                    {
                        resp.IsSportLimit = true;
                        resp.SportId = GameInfo.SportId;
                        resp.MinWager = oLimitsSportMaster.MinWager;
                        resp.MaxWager = oLimitsSportMaster.MaxWager;
                        resp.MaxPayout = oLimitsSportMaster.MaxPayout;

                        resp.MinPrice = oLimitsSportMaster.MinPrice;
                        resp.MaxPrice = oLimitsSportMaster.MaxPrice;
                        resp.TotAmtGame = oLimitsSportMaster.TotAmtGame;
                    }
                    else
                    {
                        // Validate the Player - League //
                        oReqMaster.LeagueId = GameInfo.LeagueId;
                        var oLimitsLeagueMaster = oAdminClass.GetProfileLimitsByIdAgent(oReqMaster);
                        if (oLimitsLeagueMaster != null)
                        {
                            resp.IsSportLimit = false;
                            resp.IsLeagueLimit = true;
                            resp.SportId = GameInfo.SportId;
                            resp.LeagueId = GameInfo.LeagueId;
                            resp.MinWager = oLimitsLeagueMaster.MinWager;
                            resp.MaxWager = oLimitsLeagueMaster.MaxWager;
                            resp.MaxPayout = oLimitsLeagueMaster.MaxPayout;


                            resp.MinPrice = oLimitsLeagueMaster.MinPrice;
                            resp.MaxPrice = oLimitsLeagueMaster.MaxPrice;
                            resp.TotAmtGame = oLimitsLeagueMaster.TotAmtGame;
                        }
                        else
                        {
                            resp = null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //throw ex;
                resp = null;
            }
            return resp;
        }

        private AgentLimitsHierarchyParlay GetAgentLimitsParlay(int PlayerId)
        {
            var oPlayerHierarchy = GetPlayerHierarchy(PlayerId);
            LiveDbClass oLiveClass = new LiveDbClass(MoverConnString);
            LiveAdminDbClass oAdminClass = new LiveAdminDbClass();
            AgentLimitsHierarchyParlay resp = new AgentLimitsHierarchyParlay
            {
                AgentId = oPlayerHierarchy.SubAgentId,
                MasterAgentId = oPlayerHierarchy.MasterAgentId,
                LeagueId = null,
                SportId = null,
                IsLeagueLimit = false,
                IsSportLimit = false,
                MinWager = 0,
                MaxWager = 0,
                MaxPayout = 0,

                MinPrice = 0,
                MaxPrice = 0,
                TotAmtGame = 0,
            };
            try
            {
                //ProfileLimitsResp GetProfileLimitsByIdAgent(GetProfileLimitsReq req)
                //First Validate the SubAgent Limits by Sport & League //
                //var GameInfo = oLiveClass.GetEventByFixture(FixtureId);
                GetProfileLimitsReq oReq = new GetProfileLimitsReq
                {
                    IdWagerType = 2,
                    AgentId = oPlayerHierarchy.SubAgentId,
                    SportId = -1,
                    LeagueId = null,
                    FixtureId = null
                };
                // Validate first the Player - Sport //
                var oLimitsSport = oAdminClass.GetProfileLimitsByIdAgent(oReq);
                if (oLimitsSport != null)
                {
                    //resp.IsSportLimit = true;
                    resp.SportId = -1;
                    resp.MinWager = oLimitsSport.MinWager;
                    resp.MaxWager = oLimitsSport.MaxWager;
                    resp.MaxPayout = oLimitsSport.MaxPayout;

                    resp.MinPrice = oLimitsSport.MinPrice;
                    resp.MaxPrice = oLimitsSport.MaxPrice;
                    resp.TotAmtGame = oLimitsSport.TotAmtGame;
                }
                else
                {
                    resp = null;
                }

                //Validate the master agent limits //
                if (resp == null)
                {
                    GetProfileLimitsReq oReqMaster = new GetProfileLimitsReq
                    {
                        IdWagerType = 2,
                        AgentId = oPlayerHierarchy.MasterAgentId,
                        SportId = -1,
                        LeagueId = null,
                        FixtureId = null
                    };
                    // Validate first the Player - Sport //
                    var oLimitsSportMaster = oAdminClass.GetProfileLimitsByIdAgent(oReqMaster);
                    if (oLimitsSportMaster != null)
                    {
                        //resp.IsSportLimit = true;
                        resp = new AgentLimitsHierarchyParlay
                        {
                            AgentId = oPlayerHierarchy.SubAgentId,
                            MasterAgentId = oPlayerHierarchy.MasterAgentId,
                            LeagueId = null,
                            SportId = -1,
                            IsLeagueLimit = false,
                            IsSportLimit = false,
                            MinWager = oLimitsSportMaster.MinWager,
                            MaxWager = oLimitsSportMaster.MaxWager,
                            MaxPayout = oLimitsSportMaster.MaxPayout,

                            MinPrice = oLimitsSportMaster.MinWager,
                            MaxPrice = oLimitsSportMaster.MaxWager,
                            TotAmtGame = oLimitsSportMaster.MaxPayout,

                    };
                    }
                    else
                    {
                        // Validate the Player - League //
                        resp = null;
                    }
                }
            }
            catch (Exception ex)
            {
                //throw ex;
                resp = null;
            }
            return resp;
        }

        private string GetFixtureName(WagerDetailCompleteDescriptionModel detailDescription) 
        {
            bool isTournament = IsSportWithTournament(detailDescription.FixtureId!);
            var fixtureName = "";

            if (!isTournament)
            {
                //RLM:2024.05.10, Fix, si alguno de los equipos viene vacio, buscar el nombre en bd
                if (detailDescription.HomeTeam == "" || detailDescription.VisitorTeam == "")
                {
                    var participants = new LiveDbClass(MoverConnString).GetParticipants(detailDescription.FixtureId);
                    
                    if (detailDescription.HomeTeam == "")
                       detailDescription.HomeTeam = participants?.Where(p => p.Position == 1).FirstOrDefault()?.Name;

                    if (detailDescription.VisitorTeam == "")
                       detailDescription.VisitorTeam = participants?.Where(p => p.Position == 2).FirstOrDefault()?.Name;                    
                }

                string homeTeam = GetShortName(detailDescription.HomeTeam!);
                string visitorTeam = GetShortName(detailDescription.VisitorTeam!);

                fixtureName = $"{homeTeam} vs {visitorTeam}";                
            }


            return $"{(isTournament? detailDescription.LeagueName! : fixtureName)}"; 
        }

        private string GetSportName(WagerDetailCompleteDescriptionModel detailDescription) 
        {
            return $"{(!leagueNameExceptions.Contains(detailDescription!.SportName) ? detailDescription!.SportName : $"{detailDescription!.SportName} {detailDescription.LeagueName}")}";
        }


        public LSport_EventPropDto CreateStraightWager(CreateStraightWagerModel straightWager)
        {
            try
            {
                //var PlayerData = GetPlayerData(idPlayer);

                decimal winAmount = (decimal)straightWager.PickSelected!.BsWinAmount!;
                decimal riskAmount = (decimal)straightWager.PickSelected.BsRiskAmount!;

                //RLM:2024.06.26, Recalcular siempre el WinAmount 
                //if (straightWager.PropSelected.StatusForWager == 9) //linea cambio, igual para win and risk
                {

                    // recalcualr el win
                    winAmount = StraightCalculateWin((int)straightWager.PickSelected.Odds1!, (decimal)straightWager.PickSelected.BsRiskAmount);
                }

                string formatBsWinAmount = straightWager.PickSelected!.BsWinAmount!.Value.ToString("0.##");
                winAmount = decimal.Parse(formatBsWinAmount);

                straightWager.PickSelected.BsRiskAmount = riskAmount;
                straightWager.PickSelected.BsWinAmount = winAmount;

                //insertamos el straight en las tablas auxiliares

                int idlivewager = InsertLiveWagerHeader((int)straightWager.IdPlayer!, 1, riskAmount, winAmount, "Straight", "10.1.1.1", 1, 0, (bool)straightWager.IsMobile!);

                if (idlivewager > 0)
                {

                    WagerDetailCompleteDescriptionModel wagerDetailCompleteDescriptionModel = new WagerDetailCompleteDescriptionModel {
                        SportName = straightWager.SportName,
                        HomeTeam = straightWager.HomeTeam,
                        VisitorTeam = straightWager.VisitorTeam,
                        MarketId = straightWager.PickSelected.MarketId,
                        MarketName = straightWager.PickSelected.MarketName,
                        Name = straightWager.PickSelected.Name,
                        BaseLine = straightWager.PickSelected.BaseLine,
                        Line = straightWager.PickSelected.Line1,
                        Odds1 = straightWager.PickSelected.Odds1,
                        LeagueName = straightWager.LeagueName,
                        IsTournament = straightWager.IsTournament,
                        FixtureId = (int)straightWager.FixtureId!
                    };



                    string FixtureName = GetFixtureName(wagerDetailCompleteDescriptionModel);
                    string SportName = GetSportName(wagerDetailCompleteDescriptionModel);
                    string CompleteDescription = FormatWagerDetailCompleteDescription(wagerDetailCompleteDescriptionModel, FixtureName, SportName);

                    var idlivewagerDetail = InsertLiveWagerDetail(idlivewager, (int)straightWager.FixtureId!, straightWager.PickSelected.MarketId, straightWager.PickSelected.IdL1!, straightWager.PickSelected.BaseLine!, straightWager.PickSelected.Line1!, (int)straightWager.PickSelected.Odds1!, (decimal)straightWager.PickSelected.Price!, straightWager.PickSelected.Name!, CompleteDescription, riskAmount, winAmount);

                   
                    if(idlivewagerDetail > 0)
                    {

                        string description = $"VegasLive #{idlivewager} [{straightWager.FixtureId}] {SportName} / {FixtureName}";
                        var idDgsWager = InsertDgsWagerHeader((int)straightWager.IdPlayer, riskAmount, winAmount, description, "10.0.0.0");

                        if (idDgsWager > 0)
                        {
                            InsertDgsWagerDetail(idDgsWager, CompleteDescription, CompleteDescription);
                            //actualizamos el idwager en la tabla auxiliar
                            UpdateLiveWagerHeader(idlivewager, idDgsWager);

                            straightWager.PickSelected.BsTicketNumber = idlivewager + "-" + idDgsWager;
                            straightWager.PickSelected.BsBetResult = 1000; //success

                        } else {
                            
                            DeleteLiveWager(idlivewager);
                            straightWager.PickSelected.BsTicketNumber = string.Empty;
                            straightWager.PickSelected.BsBetResult = -1;
                        }



                    }
                    else
                    {
                        
                        DeleteLiveWager(idlivewager);
                        straightWager.PickSelected.BsBetResult = -1;
                    }
                }
                else
                {
                    straightWager.PickSelected.BsBetResult = -1;
                }

            }
            catch (Exception ex)
            {

            }
            return straightWager.PickSelected!;
        }

        public LSport_BetSlipObj CreateParlayWager(LSport_BetSlipObj betslipObj)
        {
            try
            {
                List<int> ListDetailWager = new List<int>();

                var descriptionList = new List<string>();

                var PlayerData = GetPlayerData(betslipObj.IdPlayer);

                decimal winAmount = ParlayCalculateWin(betslipObj);
                decimal riskAmount = betslipObj.ParlayRiskAmount;

                string formatWinAmount = winAmount.ToString("0.##");
                winAmount = decimal.Parse(formatWinAmount);

                betslipObj.ParlayRiskAmount = riskAmount;
                betslipObj.ParlayWinAmount = winAmount;

                //insertamos el straight en las tablas auxiliares


                var numberEvents = 0;
                foreach (var item in betslipObj.Events)
                {
                    foreach (var sel in item.Selections)
                    {
                        numberEvents++;
                    }
                }

                int idlivewager = InsertLiveWagerHeader(betslipObj.IdPlayer, 2, riskAmount, winAmount, "Parlay", "10.1.1.1", numberEvents, 1, (bool)betslipObj.IsMobile!);

                if (idlivewager > 0)
                {
                    string sports = " (";
                    foreach (var item in betslipObj.Events)
                    {
                        if (item.Selections.Any())
                            sports += " • " + item.SportName;
                    }
                    sports += ")";

                    string Description = $"VegasLive #{ idlivewager } PARLAY { numberEvents} TEAMS";
 
                    foreach (var item in betslipObj.Events)
                    {
                        foreach (var sel in item.Selections)
                        { 
                            WagerDetailCompleteDescriptionModel wagerDetailCompleteDescriptionModel = new WagerDetailCompleteDescriptionModel {
                                SportName = item.SportName,
                                HomeTeam = item.HomeTeam,
                                VisitorTeam = item.VisitorTeam,
                                MarketId    = sel.MarketId,
                                MarketName = sel.MarketName,
                                Name = sel.Name,
                                BaseLine = sel.BaseLine,
                                Line = sel.Line1,
                                Odds1 = sel.Odds1,
                                IsTournament = item.IsTournament,
                                LeagueName = item.LeagueName,
                                FixtureId = item.FixtureId
                            };

                            string FixtureName = GetFixtureName(wagerDetailCompleteDescriptionModel);
                            string SportName = GetSportName(wagerDetailCompleteDescriptionModel);
                            string itemDescription = FormatWagerDetailCompleteDescription(wagerDetailCompleteDescriptionModel, FixtureName, SportName);

                            descriptionList.Add(itemDescription);

                            ListDetailWager.Add(InsertLiveWagerDetail(idlivewager, item.FixtureId, sel.MarketId, sel.IdL1, sel.BaseLine, sel.Line1, (int)sel.Odds1, (decimal)sel.Price, sel.Name, itemDescription, riskAmount, winAmount));
                        }
                    }

                    //revisamos que los detailles y el header se hayan ingresado correctamente


                    if (ListDetailWager.Count == numberEvents && ListDetailWager.All(x => x > 1))
                    {

                        var idDgsWager = InsertDgsWagerHeader(betslipObj.IdPlayer, riskAmount, winAmount, Description, "10.0.0.0");

                        if (idDgsWager > 0)
                        {
                            foreach (var item in descriptionList)
                            {
                                InsertDgsWagerDetail(idDgsWager, item, item);
                            }


                            //actualizamos el idwager en la tabla auxiliar
                            UpdateLiveWagerHeader(idlivewager, idDgsWager);

                            //  propSelected.BsTicketNumber = idlivewager + "-" + idDgsWager;
                            //  propSelected.BsBetResult = 0;
                            betslipObj.ParlayBetResult = 0;
                            betslipObj.ParlayBetTicket = idlivewager + "-" + idDgsWager;
                        }
                        else
                        {
                            DeleteLiveWager(idlivewager);
                            betslipObj.ParlayBetResult = -1;
                        }
                    }
                    else
                    {
                        DeleteLiveWager(idlivewager);
                        betslipObj.ParlayBetResult = -1;
                    }
                }
                else
                {
                    betslipObj.ParlayBetResult = -1;
                }

            }
            catch (Exception ex)
            {
            }
            return betslipObj;
        }

        public string AddParlayDetails(string desc, string value)
        {
            if (!String.IsNullOrEmpty(desc))
            {
                desc += "|" + value;
            }
            else
                desc = value;

            return desc;
        }

        public LSportStringResult InsertParlay(LSportSimpleInsertParlay paramData)
        {
            LSportStringResult result = new LSportStringResult();

            try
            {
                using var connection = new SqlConnection(DgsConnString);

                //  var parameters = new { FixtureId = paramData.FixtureId, MarketId = paramData.MarketId, Description = paramData.DetailDescription, Line = paramData.Line, Odds = paramData.Odds, Risk = paramData.Risk, Win = paramData.Win, WagerType = 1, WagerSelection = paramData.WagerSelection, IsLive = paramData.IsLive, SideName = paramData.SideName }; /*wager type = 1 straigh 2 parlays*/

                // int identityId = connection.ExecuteScalar<int>("DBA_LSports_InsertBet", parameters, commandType: CommandType.StoredProcedure);

                Int64 idWager = InsertDgsParlayWager(paramData.IdPlayer, paramData.Odds, paramData.Points, paramData.Risk, paramData.Win, paramData.IdWagerType, paramData.HeaderDescription, paramData.DetailDescription, "10.1.1.1", paramData.WagerSelectionPlay, paramData.NumTeams, paramData.KeyDetails);


                if (idWager > 0)
                {
                    //UpdateLSportBet(identityId, idWager);

                    // result.IdLSportBet = identityId;
                    result.IdWagerHeader = idWager;
                    result.Result = "Success";
                }
                //else
                //{
                //    result.IdLSportBet = identityId;
                //    result.IdWagerHeader = idWager;
                //    result.Result = "Error";
                //    //hubo un error al crear la apuesta en DGS
                //}
            }
            catch (Exception ex)
            {
                result.IdLSportBet = 0;
                result.IdWagerHeader = 0;
                result.Result = ex.Message;
                //hubo un error al crear la apuesta en DGS
            }

            return result;
        }

        private Int64 InsertDgsStraightWager(int idplayer, decimal odds, decimal points, decimal risk, decimal win, Int64 IdWagerType, string headerDescription, string detailDescription, string Ip, int WagerSelection)
        {
            Int64 idWagerInsert = 0;

            try
            {
                using var connection = new SqlConnection(DgsConnString);

                var param = new DynamicParameters();
                param.Add("@prmOnline", dbType: DbType.Byte, value: 1, direction: ParameterDirection.Input);
                param.Add("@prmFreePlay", dbType: DbType.Byte, value: 0, direction: ParameterDirection.Input);
                param.Add("@prmGraded", dbType: DbType.Byte, value: 0, direction: ParameterDirection.Input);
                param.Add("@prmLineFromAgent", dbType: DbType.Byte, value: 0, direction: ParameterDirection.Input);
                param.Add("@prmIdPlayer", dbType: DbType.Int64, value: idplayer, direction: ParameterDirection.Input);
                param.Add("@prmOdds", dbType: DbType.Int16, value: odds, direction: ParameterDirection.Input);
                param.Add("@prmVScore", dbType: DbType.Int16, value: 0, direction: ParameterDirection.Input);
                param.Add("@prmHScore", dbType: DbType.Int16, value: 0, direction: ParameterDirection.Input);

                param.Add("@prmChangedWager", dbType: DbType.Int16, value: 0, direction: ParameterDirection.Input);
                param.Add("@prmPoints", dbType: DbType.Double, value: points, direction: ParameterDirection.Input);
                param.Add("@prmBuyPoints", dbType: DbType.Int64, value: 0, direction: ParameterDirection.Input);
                param.Add("@prmAmount", dbType: DbType.Decimal, value: risk, direction: ParameterDirection.Input);
                param.Add("@prmRiskAmount", dbType: DbType.Decimal, value: risk, direction: ParameterDirection.Input);
                param.Add("@prmWinAmount", dbType: DbType.Decimal, value: win, direction: ParameterDirection.Input);
                param.Add("@prmRiskWin", dbType: DbType.Int16, value: 0, direction: ParameterDirection.Input);

                param.Add("@prmOriginalRiskAmount", dbType: DbType.Decimal, value: risk, direction: ParameterDirection.Input);


                param.Add("@prmWagerType", dbType: DbType.Int16, value: 0, direction: ParameterDirection.Input);
                param.Add("@prmJuice", dbType: DbType.Int64, value: 0, direction: ParameterDirection.Input);
                param.Add("@prmResult", dbType: DbType.Int16, value: 255, direction: ParameterDirection.Input);
                param.Add("@prmListedPitcher", dbType: DbType.Int16, value: 0, direction: ParameterDirection.Input);
                param.Add("@prmIdCall", dbType: DbType.Int64, value: 0, direction: ParameterDirection.Input);
                param.Add("@prmIdUser", dbType: DbType.Int16, value: 0, direction: ParameterDirection.Input);

                param.Add("@prmPhoneLine", dbType: DbType.Int16, value: -1, direction: ParameterDirection.Input);
                param.Add("@prmIdGame", dbType: DbType.Int16, value: 0, direction: ParameterDirection.Input);
                param.Add("@prmPlay", dbType: DbType.Int16, value: WagerSelection, direction: ParameterDirection.Input);
                param.Add("@prmIdWagerType", dbType: DbType.Int64, value: IdWagerType, direction: ParameterDirection.Input);
                param.Add("@prmIdSport", dbType: DbType.String, value: "", direction: ParameterDirection.Input);
                param.Add("@prmHeaderDescription", dbType: DbType.String, value: headerDescription, direction: ParameterDirection.Input);

                param.Add("@prmHeaderCompleteDescription", dbType: DbType.String, value: headerDescription, direction: ParameterDirection.Input);
                param.Add("@prmDetailDescription", dbType: DbType.String, value: detailDescription, direction: ParameterDirection.Input);
                param.Add("@prmDetailCompleteDescription", dbType: DbType.String, value: detailDescription, direction: ParameterDirection.Input);
                param.Add("@prmIP", dbType: DbType.String, value: Ip, direction: ParameterDirection.Input);


                param.Add("@prmLineStyle", dbType: DbType.String, value: "E", direction: ParameterDirection.Input);
                param.Add("@prmOriginalPoints", dbType: DbType.Double, value: 0, direction: ParameterDirection.Input);
                param.Add("@prmOriginalOdds", dbType: DbType.Int16, value: odds, direction: ParameterDirection.Input);
                param.Add("@prmLineFactor", dbType: DbType.Double, value: 0, direction: ParameterDirection.Input);
                param.Add("@prmDerivativePoints", dbType: DbType.Double, value: 0, direction: ParameterDirection.Input);


                param.Add("@prmoutIdWager", dbType: DbType.Int64, value: 0, direction: ParameterDirection.InputOutput);

                connection.Execute("Insert_StraightBet", param, commandType: CommandType.StoredProcedure);

                idWagerInsert = param.Get<Int64>("@prmoutIdWager");



            }
            catch (Exception ex)
            {
                string error = ex.Message;
            }
            return idWagerInsert;
        }

        private Int64 InsertDgsParlayWager(int idplayer, string odds, string points, decimal risk, decimal win, Int64 IdWagerType, string headerDescription, string detailDescription, string Ip, string WagerSelectionPlay, int NumTeams, string KeyDetails)
        {
            Int64 idWagerInsert = 0;
            string results = "";

            for (int i = 0; i < NumTeams; i++)
            {
                if (String.IsNullOrEmpty(results))
                    results = "255";
                else
                    results += "|255";
            }

            try
            {
                using var connection = new SqlConnection(DgsConnString);

                var param = new DynamicParameters();
                param.Add("@prmOnline", dbType: DbType.Byte, value: 1, direction: ParameterDirection.Input);
                param.Add("@prmFreePlay", dbType: DbType.Byte, value: 0, direction: ParameterDirection.Input);

                param.Add("@prmOpenPlay", dbType: DbType.Byte, value: 0, direction: ParameterDirection.Input);
                param.Add("@prmAmount", dbType: DbType.Decimal, value: risk, direction: ParameterDirection.Input);
                param.Add("@prmRiskAmount", dbType: DbType.Decimal, value: risk, direction: ParameterDirection.Input);
                param.Add("@prmWinAmount", dbType: DbType.Decimal, value: win, direction: ParameterDirection.Input);
                param.Add("@prmOriginalRiskAmount", dbType: DbType.Decimal, value: risk, direction: ParameterDirection.Input);
                param.Add("@prmRiskWin", dbType: DbType.Int16, value: 0, direction: ParameterDirection.Input);
                param.Add("@prmWagerType", dbType: DbType.Int16, value: 0, direction: ParameterDirection.Input);



                param.Add("@prmNumTeams", dbType: DbType.Int16, value: NumTeams, direction: ParameterDirection.Input);
                param.Add("@prmIdCall", dbType: DbType.Int64, value: 0, direction: ParameterDirection.Input);
                param.Add("@prmIdPlayer", dbType: DbType.Int64, value: idplayer, direction: ParameterDirection.Input);
                param.Add("@prmChangedWager", dbType: DbType.Int16, value: 0, direction: ParameterDirection.Input);
                param.Add("@prmTicketNumber", dbType: DbType.Int16, value: 0, direction: ParameterDirection.Input);
                param.Add("@prmIdUser", dbType: DbType.Int16, value: 0, direction: ParameterDirection.Input);
                param.Add("@prmIdWagerType", dbType: DbType.Int64, value: IdWagerType, direction: ParameterDirection.Input);
                param.Add("@prmPhoneLine", dbType: DbType.Int16, value: -1, direction: ParameterDirection.Input);
                param.Add("@prmCRRNumDetails", dbType: DbType.Int16, value: 0, direction: ParameterDirection.Input);
                param.Add("@prmHeaderDescription", dbType: DbType.String, value: headerDescription, direction: ParameterDirection.Input);
                param.Add("@prmHeaderCompleteDescription", dbType: DbType.String, value: headerDescription, direction: ParameterDirection.Input);
                param.Add("@prmIP", dbType: DbType.String, value: Ip, direction: ParameterDirection.Input);
                param.Add("@prmIdGame", dbType: DbType.Int16, value: 0, direction: ParameterDirection.Input);
                param.Add("@prmPlay", dbType: DbType.String, value: WagerSelectionPlay, direction: ParameterDirection.Input);
                param.Add("@prmBuyPoints", dbType: DbType.String, value: 0, direction: ParameterDirection.Input);
                param.Add("@prmIdSport", dbType: DbType.String, value: "", direction: ParameterDirection.Input);
                param.Add("@prmPitcher", dbType: DbType.String, value: "", direction: ParameterDirection.Input);
                param.Add("@prmOdds", dbType: DbType.String, value: odds, direction: ParameterDirection.Input);
                param.Add("@prmJuice", dbType: DbType.Int64, value: 0, direction: ParameterDirection.Input);
                param.Add("@prmGraded", dbType: DbType.Byte, value: 0, direction: ParameterDirection.Input);
                param.Add("@prmKeyDetail", dbType: DbType.String, value: KeyDetails, direction: ParameterDirection.Input);
                param.Add("@prmLineFromAgent", dbType: DbType.Byte, value: 0, direction: ParameterDirection.Input);
                param.Add("@prmIfBetAmount", dbType: DbType.String, value: "", direction: ParameterDirection.Input);
                param.Add("@prmIfBetRiskWin", dbType: DbType.String, value: "", direction: ParameterDirection.Input);
                param.Add("@prmResult", dbType: DbType.String, value: results, direction: ParameterDirection.Input);
                param.Add("@prmVScore", dbType: DbType.Int16, value: 0, direction: ParameterDirection.Input);
                param.Add("@prmHScore", dbType: DbType.Int16, value: 0, direction: ParameterDirection.Input);
                param.Add("@prmPoints", dbType: DbType.String, value: points, direction: ParameterDirection.Input);
                param.Add("@prmDetailDescription", dbType: DbType.String, value: detailDescription, direction: ParameterDirection.Input);
                param.Add("@prmDetailCompleteDescription", dbType: DbType.String, value: detailDescription, direction: ParameterDirection.Input);


                param.Add("@prmLineStyle", dbType: DbType.String, value: "E", direction: ParameterDirection.Input);
                param.Add("@prmOriginalPoints", dbType: DbType.String, value: "", direction: ParameterDirection.Input);
                param.Add("@prmOriginalOdds", dbType: DbType.String, value: odds, direction: ParameterDirection.Input);
                param.Add("@prmLineFactor", dbType: DbType.String, value: "", direction: ParameterDirection.Input);
                param.Add("@prmDerivativePoints", dbType: DbType.String, value: "", direction: ParameterDirection.Input);


                param.Add("@prmoutIdWager", dbType: DbType.Int64, value: 0, direction: ParameterDirection.InputOutput);

                connection.Execute("Insert_Parlay", param, commandType: CommandType.StoredProcedure);

                idWagerInsert = param.Get<Int64>("@prmoutIdWager");
            }
            catch (Exception ex)
            {

            }
            return idWagerInsert;
        }

        private LineChangedDto CompareLine(LSport_EventValuesDto betslipLine, LSport_EventValuesDto currentLine, int type)
        {
            //   currentLine = new Dap_LSportClass().ConvertStrValues(currentLine);

            LineChangedDto lineChangeData = new LineChangedDto
            {
                LineType = 1, /*Line*/
                LineChanged = false
            };
            try
            {
                if (type == 1) //visitor spread
                {
                    if (betslipLine.VisitorSpread != currentLine.VisitorSpread || betslipLine.VisitorSpreadOdds != currentLine.VisitorSpreadOdds)
                    {
                        lineChangeData.LineChanged = true;
                        lineChangeData.Message1 = "Line changed";
                        lineChangeData.Message2 = "from: " + betslipLine.VisitorSpreadStr + " to: " + currentLine.VisitorSpreadStr;
                    }
                }
                else if (type == 2) //visitor total
                {
                    if (betslipLine.TotalOver != currentLine.TotalOver || betslipLine.Total != currentLine.Total)
                    {
                        lineChangeData.LineChanged = true;
                        lineChangeData.Message1 = "Line changed";
                        lineChangeData.Message2 = "from: " + betslipLine.VisitorTotalStr + " to: " + currentLine.VisitorTotalStr;
                    }
                }
                else if (type == 3) //visitor ML
                {
                    if (betslipLine.VisitorML != currentLine.VisitorML)
                    {
                        lineChangeData.LineChanged = true;
                        lineChangeData.Message1 = "Line changed";
                        lineChangeData.Message2 = "from: " + betslipLine.VisitorMLStr + " to: " + currentLine.VisitorMLStr;
                    }
                }
                else if (type == 4) //Home Spread
                {
                    if (betslipLine.HomeSpread != currentLine.HomeSpread || betslipLine.HomeSpreadOdds != currentLine.HomeSpreadOdds)
                    {
                        lineChangeData.LineChanged = true;
                        lineChangeData.Message1 = "Line changed";
                        lineChangeData.Message2 = "from: " + betslipLine.HomeSpreadStr + " to: " + currentLine.HomeSpreadStr;
                    }
                }
                else if (type == 5) //home total
                {
                    if (betslipLine.TotalUnder != currentLine.TotalUnder || betslipLine.Total != currentLine.Total)
                    {
                        lineChangeData.LineChanged = true;
                        lineChangeData.Message1 = "Line changed";
                        lineChangeData.Message2 = "from: " + betslipLine.HomeTotalStr + " to: " + currentLine.HomeTotalStr;
                    }
                }
                else if (type == 6) //home ML
                {
                    if (betslipLine.HomeML != currentLine.HomeML)
                    {
                        lineChangeData.LineChanged = true;
                        lineChangeData.Message1 = "Line changed";
                        lineChangeData.Message2 = "from: " + betslipLine.HomeMLStr + " to: " + currentLine.HomeMLStr;
                    }
                }

            }
            catch (Exception ex)
            {

            }

            return lineChangeData;
        }

        private LineChangedDto CompareProp(LSport_EventPropDto betslipProp, CompletePropMarket currentProp, int type)
        {
            //currentLine = new Dap_LSportClass().ConvertStrValues(currentLine);

            LineChangedDto lineChangeData = new LineChangedDto
            {
                LineType = 2, /*Prop*/
                LineChanged = false
            };
            try
            {
                if (type == 10) //Total Over
                {
                    if (betslipProp.Odds1 != Convert.ToInt32(currentProp.PriceUS))
                    {
                        lineChangeData.LineChanged = true;
                        lineChangeData.Message1 = "Prop changed";
                        lineChangeData.Message2 = "from: " + betslipProp.Odds1 + " to: " + currentProp.PriceUS;
                    }
                }
                //else if (type == 11) //Total Under
                //{
                //    if (betslipProp.Odds2 != currentProp.Odds2)
                //    {
                //        lineChangeData.LineChanged = true;
                //        lineChangeData.Message1 = "Prop changed";
                //        lineChangeData.Message2 = "from: " + betslipProp.Odds2 + " to: " + currentProp.Odds2;
                //    }
                //}
                //else if (type == 12) //Spread away
                //{
                //    if (betslipProp.Odds1 != betslipProp.Odds2)
                //    {
                //        lineChangeData.LineChanged = true;
                //        lineChangeData.Message1 = "Line changed";
                //        lineChangeData.Message2 = "from: " + betslipProp.Odds1 + " to: " + currentProp.Odds1;
                //    }
                //}
                //else if (type == 13) //Spread Home
                //{
                //    if (betslipProp.Odds2 != betslipProp.Odds2)
                //    {
                //        lineChangeData.LineChanged = true;
                //        lineChangeData.Message1 = "Line changed";
                //        lineChangeData.Message2 = "from: " + betslipProp.Odds2 + " to: " + currentProp.Odds2;
                //    }
                //}

            }
            catch (Exception ex)
            {

            }

            return lineChangeData;
        }

        private CompletePropMarket GetPropValues(int FixtureId, string IdLine)
        {
            CompletePropMarket PropValue = new CompletePropMarket();
            try
            {
                using var connection = new SqlConnection(MoverConnString);
                PropValue = connection.Query<CompletePropMarket>("SELECT Id, FixtureId, MarketId, MarketName, MainLine, Name, Status, StartPrice, Price, PriceUS, BaseLine, Line FROM tb_MGL_FixtureBetLines where Fixtureid = @fixtureId and status = 1 and id = @Id",
                    new { fixtureId = FixtureId, Id = IdLine }).FirstOrDefault();
            }
            catch (Exception ex)
            {

            }
            return PropValue;
        }

        private PlayerDto GetPlayerData(int IdPlayer)
        {
            PlayerDto playerData = new PlayerDto();
            LiveAdminDbClass oLiveAdmin = new LiveAdminDbClass();
            try
            {
                var oPlayerHierarchy = GetPlayerHierarchy(IdPlayer);
                //Verificamos acceso del master agent //
                if (!oLiveAdmin.GetAccessDeniedById(new GetAccessDeniedListReq() { AgentId = oPlayerHierarchy.MasterAgentId, PlayerId = null, AllData = false }).Enable)
                {
                    playerData.Access = false;
                    playerData.IdProfile = -1;
                    playerData.Player = "";
                    playerData.IdPlayer = -1;
                }
                else if (!oLiveAdmin.GetAccessDeniedById(new GetAccessDeniedListReq() { AgentId = oPlayerHierarchy.SubAgentId, PlayerId = null, AllData = false }).Enable)
                {
                    playerData.Access = false;
                    playerData.IdProfile = -1;
                    playerData.Player = "";
                    playerData.IdPlayer = -1;
                }
                else if (!oLiveAdmin.GetAccessDeniedById(new GetAccessDeniedListReq() { AgentId = oPlayerHierarchy.SubAgentId, PlayerId = IdPlayer, AllData = false }).Enable)
                {
                    playerData.Access = false;
                    playerData.IdProfile = -1;
                    playerData.Player = "";
                    playerData.IdPlayer = -1;
                }
                else
                {
                    using var connection = new SqlConnection(DgsConnString);
                    playerData = connection.Query<PlayerDto>("SELECT IdPlayer, Player, IdProfile FROM [DGSData].[dbo].[PLAYER] WHERE IDPLAYER = @IdPlayer", new { IdPlayer }).FirstOrDefault();
                    playerData.Access = true;
                }
            }
            catch (Exception ex)
            {
                _ = ex.Message;
            }
            return playerData;
        }

        public PlayerHierarchy GetPlayerHierarchy(int IdPlayer)
        {
            PlayerHierarchy resp = new PlayerHierarchy
            {
                PlayerId = -1,
                SubAgentId = -1,
                MasterAgentId = -1
            };
            LiveAdminDbClass oLiveAdmin = new LiveAdminDbClass();
            try
            {
                var PlayerInfo = oLiveAdmin.GetPlayerInfo(new GetPlayerInfoReq() { PlayerId = IdPlayer });
                var AgentList = oLiveAdmin.GetAgentTree(new GetAgentHierarchyReq() { IdAgent = PlayerInfo.IdAgent });
                var oAgent = new AgentTreeResp();
                var oMasterAgent = new AgentTreeResp();
                if (AgentList != null)
                {
                    oAgent = AgentList.Where(x => x.IdAgent == PlayerInfo.IdAgent).FirstOrDefault();
                    if (oAgent != null && oAgent.AgentLevel == 2)
                    {
                        oMasterAgent = oAgent;
                    }
                    else
                    {
                        oMasterAgent = AgentList.Where(x => x.AgentLevel == 2).FirstOrDefault();
                    }
                }
                resp.PlayerId = IdPlayer;
                resp.SubAgentId = PlayerInfo.IdAgent;
                resp.MasterAgentId = oMasterAgent != null ? oMasterAgent.IdAgent : -1;
            }
            catch (Exception ex)
            {
                _ = ex.Message;
            }
            return resp;
        }

        private int GetIdWagerType(int IdProfile, int WagerType)
        {
            int idWt = 0;
            try
            {
                using var connection = new SqlConnection(DgsConnString);
                idWt = connection.Query<Int32>("select IdWagerType from WAGERTYPE where IdProfile= @IdProfile and WagerType= @wagerType", new { IdProfile, WagerType }).FirstOrDefault();
            }
            catch (Exception ex)
            {
            }
            return idWt;
        }

        public int InsertDgsWagerHeader(int IdPlayer, decimal RiskAmount, decimal OriginalWinAmount, string Description, string Ip)
        {
            int resp = 0;
            try
            {
                using (var connection = new SqlConnection(DgsConnString))
                {
                    var procedure = "[VegasLive_InsertBet]";
                    var values = new
                    {
                        IdPlayer = IdPlayer,
                        RiskAmount = RiskAmount,
                        OriginalWinAmount = OriginalWinAmount,
                        CompleteDescription = Description.Length >= 100 ? Description.Substring(0, 99) : Description, //100
                        Description = Description.Length >= 255 ? Description.Substring(0, 254) : Description, // 255
                        IPAddress = Ip

                    };
                    resp = connection.Query<int>(procedure, values, commandType: CommandType.StoredProcedure).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                resp = -1;
            }
            return resp;
        }

        public int InsertDgsWagerDetail(int idwager, string CompleteDescription/*100*/, string Description/*255*/)
        {
            int resp = 0;
            try
            {
                using (var connection = new SqlConnection(DgsConnString))
                {
                    var procedure = "[VegasLive_InsertBetDetail]";
                    var values = new
                    {
                        IdWager = idwager,
                        CompleteDescription = CompleteDescription.Length >= 100 ? CompleteDescription.Substring(0, 99) : CompleteDescription,
                        Description = Description,

                    };
                    resp = connection.Query<int>(procedure, values, commandType: CommandType.StoredProcedure).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                resp = -1;
            }
            return resp;
        }

        private int UpdateLiveWagerHeader(int idLiveWager, int dgsIdWager)
        {
            int idWt = 0;
            try
            {
                using var connection = new SqlConnection(MoverConnString);
                connection.Query<string>(@"Update tb_MGL_WagerHeader SET DgsIdWager = " + dgsIdWager + "  WHERE IdLiveWager = " + idLiveWager).FirstOrDefault();
            }
            catch (Exception ex)
            {
            }
            return idWt;
        }

        public int InsertLiveWagerHeader(int IdPlayer, int IdWagerType, decimal RiskAmount, decimal WinAmount, string Description, string Ip, int NumDetails, Int64 DgsIdWager, bool isMobile)
        {
            int resp = 0;
            try
            {
                using (var connection = new SqlConnection(MoverConnString))
                {
                    var procedure = "[sp_MGL_InsertWagerHeader]";
                    var values = new
                    {
                        IdPlayer = IdPlayer,
                        IdWagerType = IdWagerType,
                        RiskAmount = RiskAmount,
                        WinAmount = WinAmount,
                        Description = Description,
                        Ip = Ip,
                        NumDetails = NumDetails,
                        DgsIdWager = DgsIdWager,
                        IsMobile = isMobile
                    };
                    resp = connection.Query<int>(procedure, values, commandType: CommandType.StoredProcedure).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                resp = -1;
            }
            return resp;
        }

        public int InsertLiveWagerDetail(int IdLiveWager, int FixtureId, int MarketId, string LineId, string BaseLine, string Line, int Odds, decimal Price, string PickTeam, string CompleteDescription, decimal RiskAmount, decimal WinAmount)
        {
            int resp = 0;
            try
            {
                using (var connection = new SqlConnection(MoverConnString))
                {
                    var procedure = "[sp_MGL_InsertWagerDetail]";
                    var values = new
                    {
                        IdLiveWager = IdLiveWager,
                        FixtureId = FixtureId,
                        MarketId = MarketId,
                        LineId = LineId,
                        BaseLine = BaseLine,
                        Line = Line,
                        Odds = Odds,
                        Price = Price,
                        PickTeam = PickTeam,
                        CompleteDescription = CompleteDescription,
                        RiskAmount = RiskAmount,
                        WinAmount = WinAmount
                    };
                    resp = connection.Query<int>(procedure, values, commandType: CommandType.StoredProcedure).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                resp = -1;
            }
            return resp;
        }

        public int DeleteLiveWager(int idLiveWager)
        {
            int resp = 0;

            try
            {
                using (var connection = new SqlConnection(MoverConnString))
                {
                    var procedure = "[sp_MGL_DeleteLiveWager]";

                    var values = new
                    {
                        IdLiveWager = idLiveWager,
                    };

                    resp = connection.Query<int>(procedure, values, commandType: CommandType.StoredProcedure).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                resp = -1;
            }

            return resp;
        }
        public PlayerInfoDto GetPlayerInfo(int idplayer, int idCall = 0)
        {
            PlayerInfoDto resp = new PlayerInfoDto();
            LiveAdminDbClass oLiveAdmin = new LiveAdminDbClass();
            try
            {
                if (idplayer > 0) {
                     var oPlayerHierarchy = GetPlayerHierarchy(idplayer);
                    //Verificamos acceso del master agent //
                    if (oLiveAdmin.GetAccessDeniedById(new GetAccessDeniedListReq() { AgentId = oPlayerHierarchy.MasterAgentId, PlayerId = null, AllData = false }) != null && !oLiveAdmin.GetAccessDeniedById(new GetAccessDeniedListReq() { AgentId = oPlayerHierarchy.MasterAgentId, PlayerId = null, AllData = false }).Enable)
                    {
                        resp.Access = false;
                        resp.IdProfile = -1;
                        resp.Player = "";
                        resp.IdPlayer = -1;
                    }
                    else if (oLiveAdmin.GetAccessDeniedById(new GetAccessDeniedListReq() { AgentId = oPlayerHierarchy.SubAgentId, PlayerId = null, AllData = false }) != null && !oLiveAdmin.GetAccessDeniedById(new GetAccessDeniedListReq() { AgentId = oPlayerHierarchy.SubAgentId, PlayerId = null, AllData = false }).Enable)
                    {
                        resp.Access = false;
                        resp.IdProfile = -1;
                        resp.Player = "";
                        resp.IdPlayer = -1;
                    }
                    else if (oLiveAdmin.GetAccessDeniedById(new GetAccessDeniedListReq() { AgentId = oPlayerHierarchy.SubAgentId, PlayerId = idplayer, AllData = false }) != null && !oLiveAdmin.GetAccessDeniedById(new GetAccessDeniedListReq() { AgentId = oPlayerHierarchy.SubAgentId, PlayerId = idplayer, AllData = false }).Enable)
                    {
                        resp.Access = false;
                        resp.IdProfile = -1;
                        resp.Player = "";
                        resp.IdPlayer = -1;
                    }
                    else if ( idCall.Equals(0) )
                    {
                        using (var connection = new SqlConnection(DgsConnString))
                        {
                            var procedure = "[CORE_GETPLAYERINFO]";
                            var values = new
                            {
                                idplayer
                            };
                            resp = connection.Query<PlayerInfoDto>(procedure, values, commandType: CommandType.StoredProcedure).FirstOrDefault();
                            if (resp != null) {
                                resp.Access = true;
                            }
                            
                        }
                    }
                    else
                    {
                        using (var connection = new SqlConnection(DgsConnString))
                        {
                            var procedure = "[CORE_GETPLAYERINFOBYIDCALL]";
                            var values = new
                            {
                                idplayer,
                                idCall
                            };
                            resp = connection.Query<PlayerInfoDto>(procedure, values, commandType: CommandType.StoredProcedure).FirstOrDefault();

                            if (resp != null) {
                                resp.Access = true;
                            }
                            
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                resp = null;
            }
            return resp;
        }

        private PlayerLimitsDto GetPlayerLimits(int idPlayer)
        {
            PlayerLimitsDto limits = new PlayerLimitsDto();
            try
            {
                using var connection = new SqlConnection(MoverConnString);
                limits = connection.Query<PlayerLimitsDto>(@"SELECT Id
                                                                  ,PlayerId
                                                                  ,IdWagerType
                                                                  ,SportId
                                                                  ,LeagueId
                                                                  ,FixtureId
                                                                  ,MaxWager
                                                                  ,MinWager
                                                                  ,MaxPayout
                                                                  ,MinPayout
                                                              FROM [tb_MGL_Profile_Limits_Players] WHERE PLAYERID = @idPlayer", new { idPlayer }).FirstOrDefault();
            }
            catch (Exception ex)
            {
                _ = ex.Message;
            }
            return limits;
        }

        private PlayerTotalWagerDto GetTodayWinAmount(int idPlayer)
        {
            PlayerTotalWagerDto limits = new PlayerTotalWagerDto();
            try
            {
                using var connection = new SqlConnection(MoverConnString);
                limits = connection.Query<PlayerTotalWagerDto>(@"SELECT sum(WinAmount) as WinAmount, sum(RiskAmount) as RiskAmount  FROM [Mover].[dbo].[tb_MGL_WagerHeader]
                  where idplayer = @idPlayer and  CAST(PlacedDateTime AS DATE) = CAST(getdate() AS DATE) 
                  group by CAST(PlacedDateTime AS DATE)", new { idPlayer }).FirstOrDefault();
            }
            catch (Exception ex)
            {
            }
            return limits;
        }

        public PlayerDtoStream GetPlayerDataStreaming(int IdPlayer)
        {
            PlayerDtoStream PlayerDtostream = new PlayerDtoStream();
            LiveAdminDbClass oLiveAdmin = new LiveAdminDbClass();
            try
            {       // var oPlayerHierarchy = GetPlayerHierarchy(IdPlayer);
                using var connection = new SqlConnection(DgsConnString);
                PlayerDtostream = connection.Query<PlayerDtoStream>("SELECT IdPlayer, Player, Password, IdProfile FROM [DGSData].[dbo].[PLAYER] WHERE IDPLAYER = @IdPlayer", new { IdPlayer }).FirstOrDefault();
                //PlayerDtostream.Access = true;

            }
            catch (Exception ex)
            {
                _ = ex.Message;
            }
            return PlayerDtostream;
        }

        public List<MoverWagerHeaderDto> GetPendingLiveWagers()
        {
            List<MoverWagerHeaderDto> wagers = new List<MoverWagerHeaderDto>();
            try
            {
                wagers = GetPendingWagerHeader();

                foreach (var wager in wagers)
                {
                    wager.Details = GetWagerDetails(wager.IdLiveWager);

                    /* RLM:2024.04.09, ya esto no es necearia, se carga en el header directamente                      
                        var playerdetails = GetPlayerInfoForLiveWagers(wager.IdPlayer);
                        wager.Player = playerdetails.Player;
                        wager.Agent = playerdetails.Agent;
                    */
                }
            }
            catch (Exception)
            {

            }
            return wagers;
        }


        public List<MoverWagerHeaderDto> GetPendingWagerHeader()
        {
            List<MoverWagerHeaderDto> wagers = new List<MoverWagerHeaderDto>();

            try
            {
                using (var connection = new SqlConnection(MoverConnString))
                {
                    //RLM:2024.04.09, se mueve a un sp, y se modifica el SP para q solo tanga juegos en status final, adicionalmente ahora retorna el nombre del agente y del jugador en la misma consulta                    
                    //wagers = connection.Query<MoverWagerHeaderDto>(@"SELECT IdLiveWager, IdPlayer, PlacedDateTime, IdWagerType, RiskAmount, WinAmount, Description, Result, Graded, Ip, NumDetails, DgsIdWager FROM tb_MGL_WagerHeader WHERE GRADED = 0 and DgsIdWager <> -1").ToList();
                    wagers = connection.Query<MoverWagerHeaderDto>("sp_MGL_GetPendingBetsHeaders", commandType: CommandType.StoredProcedure).ToList();
                    
                }
            }
            catch (Exception ex)
            {
             //   _ = new Misc().WriteErrorLog("MoverDbClass", "GetPendingWagerHeader", ex.Message, ex.StackTrace);
            }

            return wagers;
        }

        public List<MoverWagerDetailDto> GetWagerDetails(int idLiveWager)
        {
            List<MoverWagerDetailDto> wagers = new List<MoverWagerDetailDto>();

            try
            {
                using (var connection = new SqlConnection(MoverConnString))
                {
                    //RLM:2024.04.09, se mueve a un sp
                    //wagers = connection.Query<MoverWagerDetailDto>(@"SELECT IdLiveWagerDetail, IdLiveWager, FixtureId, MarketId, LineId, BaseLine, Line, Odds, Price, PickTeam, Result, CompleteDescription, RiskAmount, WinAmount FROM TB_MGL_WAGERDETAIL WHERE IDLIVEWAGER=" + idLiveWager).ToList();

                    var parameters = new
                    {
                        idLiveWager = idLiveWager
                    };
                    wagers = connection.Query<MoverWagerDetailDto>("sp_MGL_GetPendingBetsDetails", parameters, commandType: CommandType.StoredProcedure).ToList();
                }
            }
            catch (Exception ex)
            {
              //  _ = new Misc().WriteErrorLog("MoverDbClass", "GetWagerDetails", ex.Message, ex.StackTrace);
            }

            return wagers;
        }

        public PlayerInfo GetPlayerInfoForLiveWagers(int idPlayer)
        {
            PlayerInfo playerInfo = new PlayerInfo();

            try
            {
                using (var connection = new SqlConnection(DgsConnString))
                {
                    playerInfo = connection.Query<PlayerInfo>(@"SELECT PL.Player, Ag.Agent, PL.IdPlayer, AG.IdAgent
                                                                        FROM
	                                                                        PLAYER PL JOIN
	                                                                        AGENT AG ON PL.IdAgent = AG.IdAgent
                                                                        WHERE IDPLAYER =" + idPlayer).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                //  _ = new Misc().WriteErrorLog("MoverDbClass", "GetWagerDetails", ex.Message, ex.StackTrace);
            }

            return playerInfo;
        }

        public async Task<int> UpdateWagerDetailResult(int IdLiveWagerDetail, int IdLiveWager, int Result, int IdUser)
        { 
            //TODO revisar si el idUser es valido!
            int result = 0;
            try
            {
                using (var connection = new SqlConnection(MoverConnString))
                {
                    var parameters = new
                    {
                        Result,
                        IdUserManualGrade = IdUser,
                        IdLiveWagerDetail,
                        IdLiveWager
                    };

                    var procedure = "[dbo].[sp_MGL_UpdateWagerDetail]";

                    await connection.OpenAsync();

                    result = await connection.ExecuteAsync(procedure, parameters, commandType: CommandType.StoredProcedure);

                }
            }
            catch (Exception ex)
            {
                result = -1;
            }
            
            return result;
        }

        public List<CheckListLines> GetLSportsBetsInfo(List<CheckListLines> checkList)
        {
            var snapShotResult = new RestApiClass().GetLSportsSnapshot(checkList, "1245", "administracion@corporacionzircon.com", "J83@d784cE");

            var snapShot = snapShotResult.SnapShot;

            if (snapShot != null)
            {
                if (snapShot.Body != null && snapShot.Body.Count > 0)
                {
                    foreach (var body in snapShot.Body)
                    {
                        if(body != null)
                        {
                            foreach (var item in checkList)
                            {
                                if (item.FixtureId == body.FixtureId && body.Markets != null)
                                {
                                    var market = body.Markets.FirstOrDefault(m => m.Id == item.MarketId);
                                    if (market != null && market.Bets != null && market.Bets.Count() > 0)
                                    {
                                        item.BetInfo = market.Bets.FirstOrDefault(b => b.Id.ToString() == item.BetId.ToString());

                                        // Si la linea no esta abierta...
                                        if (null != item.BetInfo && item.BetInfo.Status != 1)
                                        {
                                            // buscar una linea de la misma apuesta q este Open
                                            var altList = market.Bets.Where(b => b.Name == item.BetInfo.Name && b.Status == 1);

                                            // solamente si hay una linea activa
                                            if (null != altList && altList.Count() == 1)
                                            {                                                
                                                item.BetInfo = altList.FirstOrDefault();  //nueva linea

                                                // Dejamos en AlternateId el Id de la nueva linea
                                                item.BetInfo.AlternateId = item.BetInfo.Id;  //nueva Id de linea

                                                
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }//end foreach
                }
                else
                {
                    //cancelar todas las lineas porque no obtuvimos datos
                }
            }
            else
            {
                //cancelar todas las lineas porque no obtuvimos datos
            }

            return checkList;
        }

        private string GetShortName(string teamName) 
        {
            teamName = teamName.Trim();
            if (string.IsNullOrEmpty(teamName))
                return "";

            var teamWords = teamName.Split(" ");

            var N = teamWords.Length;

            if (N == 1)
                return teamName;

            int k = N - 1;
            while (k > 0)
            {
                if (teamWords[k].Length > 3)
                    break;
                else
                    k--;
            }

            string abbreviations = "";
            for (int i = 0; i < k; i++)
            {
                abbreviations += teamWords[i].ToUpper()[0];
            }

            for (int i = k; i < N; i++)
            {
                abbreviations = abbreviations+" "+teamWords[i];
            }

            return abbreviations;
        }

        private string FormatWagerDetailCompleteDescription(WagerDetailCompleteDescriptionModel wagerDetailDescription, string fixtureName, string sportName) 
        {   
            string line = wagerDetailDescription.Line.IsNullOrEmpty() ? "" : $"{wagerDetailDescription.Line}" ;
            string baseLine = wagerDetailDescription.BaseLine.IsNullOrEmpty() ? "" : $"{wagerDetailDescription.BaseLine}" ;                        

            if (line.Replace("-", "").Equals(baseLine.Replace("-", "")) )            
                baseLine = "";
            else 
                baseLine = " "+baseLine;            

            string[] lineValues = line.Split(' ');
            string leftValue = lineValues[0];

            if (!double.TryParse(leftValue, out double doubleValue))            
                doubleValue = 0;


            var isSpread = SpreadMarkets.Contains(wagerDetailDescription.MarketId);

            if (doubleValue > 0 && isSpread)
                line = $"+{line}";

            string odds = wagerDetailDescription.Odds1 > 0 ? $"+{wagerDetailDescription.Odds1}" : $"{wagerDetailDescription.Odds1}";            
            

            return $"{wagerDetailDescription!.MarketName}:{baseLine} {wagerDetailDescription.Name} {line} {odds} [{ fixtureName }/{ sportName }]";
        }

        public bool IsSportWithTournament(int fixtureId)
        {
            bool response;

            try
            {
                using (var connection = new SqlConnection(MoverConnString))
                {
                    var procedure = "[sp_MGL_IsSportWithTournament]";

                    var values = new
                    {
                        fixtureId,
                    };

                    response = connection.Query<bool>(procedure, values, commandType: CommandType.StoredProcedure).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                response = false;
            }

            return response;
        }

    }//end class

    public class GradeDetailWager
    {
        public int IdLiveWagerDetail { get; set; }
        public int IdLiveWager { get; set; }
        public int Result { get; set; }
        public int IdUser { get; set; }
    }

    public class PlayerInfo
    {
        public string Player { get; set; }
        public int IdPlayer { get; set; }
        public string Agent { get; set; }
        public int IdAgent { get; set; }
    }

    public class PlayerHierarchy
    {
        public int PlayerId { get; set; }
        public int SubAgentId { get; set; }
        public int MasterAgentId { get; set; }
    }

    public class PlayerLimitsHierarchyStraight
    {
        public int PlayerId { get; set; }
        public int? SportId { get; set; }
        public int? LeagueId { get; set; }
        public bool IsSportLimit { get; set; }
        public bool IsLeagueLimit { get; set; }
        public decimal MinWager { get; set; }
        public decimal MaxWager { get; set; }
        public decimal MaxPayout { get; set; }
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public decimal TotAmtGame { get; set; }
    }

    public class AgentLimitsHierarchyParlay
    {
        public int AgentId { get; set; }
        public int MasterAgentId { get; set; }
        public int? SportId { get; set; }
        public int? LeagueId { get; set; }
        public bool IsSportLimit { get; set; }
        public bool IsLeagueLimit { get; set; }
        public decimal MinWager { get; set; }
        public decimal MaxWager { get; set; }
        public decimal MaxPayout { get; set; }
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public decimal TotAmtGame { get; set; }
    }

    public class AgentLimitsHierarchyStraight
    {
        public int AgentId { get; set; }
        public int MasterAgentId { get; set; }
        public int? SportId { get; set; }
        public int? LeagueId { get; set; }
        public bool IsSportLimit { get; set; }
        public bool IsLeagueLimit { get; set; }
        public decimal MinWager { get; set; }
        public decimal MaxWager { get; set; }
        public decimal MaxPayout { get; set; }
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public decimal TotAmtGame { get; set; }
    }

    public class PlayerLimitsHierarchyParlay
    {
        public int PlayerId { get; set; }
        public int? SportId { get; set; }
        public int? LeagueId { get; set; }
        public bool IsSportLimit { get; set; }
        public bool IsLeagueLimit { get; set; }
        public decimal MinWager { get; set; }
        public decimal MaxWager { get; set; }
        public decimal MaxPayout { get; set; }
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public decimal TotAmtGame { get; set; }
    }

    public class PlayerTotalWagerDto
    {
        public decimal WinAmount { get; set; }
        public decimal RiskAmount { get; set; }
    }

    public class PlayerLimitsDto
    {
        public int Id { get; set; }
        public int PlayerId { get; set; }
        public int IdWagerType { get; set; }
        public int SportId { get; set; }
        public int LeagueId { get; set; }
        public int FixtureId { get; set; }
        public decimal MaxWager { get; set; }
        public decimal MinWager { get; set; }
        public decimal MaxPayout { get; set; }
        public decimal MinPayout { get; set; }

    }

    public class PlayerInfoDto
    {
        public string Player { get; set; }
        public int IdPlayer { get; set; }
        public int WagerType { get; set; }
        public List<string> Leagues { get; set; }
        public int IdWagerType { get; set; }
        public int IdBook { get; set; }
        public int IdProfile { get; set; }
        public int IdProfileLimits { get; set; }
        public int IdLanguage { get; set; }
        public string NhlLine { get; set; }
        public string MblLine { get; set; }
        public int IdLineType { get; set; }
        public string LineStyle { get; set; }
        public float UTC { get; set; }
        public int IdTimeZone { get; set; }
        public string TimeZoneDesc { get; set; }
        public int IdAgent { get; set; }
        public Decimal CurrentBalance { get; set; }
        public Decimal AmountAtRisk { get; set; }
        public Decimal Available { get; set; }
        public int IdCurrency { get; set; }
        public string Currency { get; set; }
        public string CurrencyDesc { get; set; }
        public int PitcherDefault { get; set; }
        public int GMT { get; set; }
        public bool Access { get; set; }
        //  public string Password { get; set; }
        public int SecondsDelay { get; set; }
    }



    public class RespPlayerLastBet
    {
        public int idPlayer { get; set; }
        public DateTime PlacedDateTime { get; set; }
        public int HoursSinceLastBet { get; set; }
    }

    public class ReqPlayerLastBet
    {
        public int idPlayer { get; set; }
    }


    public class LSportStringResult
    {
        public Int64 IdWagerHeader { get; set; }
        public Int64 IdLSportBet { get; set; }
        public string Result { get; set; }
    }

    public class LSportSimpleInsertStraight
    {
        public string HeaderDescription { get; set; }
        public string DetailDescription { get; set; }
        public int MarketId { get; set; }
        public int FixtureId { get; set; }
        public Decimal Line { get; set; }
        public Decimal Odds { get; set; }
        public Decimal Risk { get; set; }
        public Decimal Win { get; set; }
        public int WagerSelection { get; set; }
        public int IdPlayer { get; set; }
        public int IdWagerType { get; set; }
        public int IsLive { get; set; }
        public string SideName { get; set; }
    }

    public class LSportSimpleInsertParlay
    {
        public string HeaderDescription { get; set; }
        public string DetailDescription { get; set; }
        public int MarketId { get; set; }
        public int FixtureId { get; set; }
        public string Points { get; set; }
        public string Odds { get; set; }
        public Decimal Risk { get; set; }
        public Decimal Win { get; set; }
        public string WagerSelectionPlay { get; set; }
        public int IdPlayer { get; set; }
        public int IdWagerType { get; set; }
        public int IsLive { get; set; }
        public string SideName { get; set; }
        public int NumTeams { get; set; }
        public string KeyDetails { get; set; }
    }

    public class PlayerDto
    {
        public int IdPlayer { get; set; }
        public string? Player { get; set; }
        public int IdProfile { get; set; }
        public bool Access { get; set; }
    }

    public class PlayerDtoStream
    {
        public int IdPlayer { get; set; }
        public string? Player { get; set; }
        public string? Password { get; set; }
        public int IdProfile { get; set; }
        public bool Access { get; set; }
    }
}//end namespace
