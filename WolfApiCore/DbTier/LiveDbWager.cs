using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using WolfApiCore.LSportApi;
using WolfApiCore.Models;
using static WolfApiCore.Models.AdminModels;

namespace WolfApiCore.DbTier
{
    public class LiveDbWager
    {
        private readonly string dgsConnString = "Data Source=192.168.11.29;Initial Catalog=DGSDATA;Persist Security Info=True;User ID=Payments;Password=p@yM3nts2701;TrustServerCertificate=True";
        private readonly string moverConnString = "Data Source=192.168.11.29;Initial Catalog=mover;Persist Security Info=True;User ID=live;Password=d_Ez*gIb8v7NogU;TrustServerCertificate=True";
        
        private readonly AppConfig _appConfig = new AppConfig();


        public LSport_BetSlipObj ValidateSelectionsForWagers(LSport_BetSlipObj Betslip)
        {
            var validForParlay = true;

            // if (Betslip.IdPlayer != 300563)
            //     return Betslip;

            var cl = new List<CheckListLines>();

            //validamos todos
            foreach (var Game in Betslip.Events)
            {
                foreach (var propSelected in Game.Selections)
                {
                    cl.Add(new CheckListLines { FixtureId = Game.FixtureId, MarketId = propSelected.MarketId, BetId = Convert.ToInt64(propSelected.IdL1) });
                }
            }

            var linesChecked = CheckLinesAlive(cl);


            foreach (var Game in Betslip.Events)
            {
                foreach (var propSelected in Game.Selections)
                {
                    foreach (var lineChecked in linesChecked)
                    {
                        if (lineChecked.BetId == Convert.ToInt64(propSelected.IdL1))
                        {

                            var dbProp = validateProp(propSelected, lineChecked.BetInfo, Betslip.AcceptLineChange, Game.FixtureId, Betslip.IdPlayer);
                            propSelected.Odds1 = dbProp.Odds1;
                            propSelected.IdL1 = dbProp.IdL1;
                            propSelected.BaseLine = dbProp.BaseLine;
                            propSelected.BsMessage = dbProp.BsMessage;
                            propSelected.BsBetResult = dbProp.BsBetResult;
                            if (dbProp.StatusForWager != 9 && dbProp.StatusForWager != 10)
                                validForParlay = false;
                        }
                    }
                }
            }


            //INSERT STRAIGHTS
            foreach (var game in Betslip.Events)
            {
                foreach (var propSelected in game.Selections)
                {
                    if (propSelected.BsWinAmount != null)
                    {
                        if (propSelected.BsWinAmount > 0)
                        {
                            if ((int)propSelected.BsRiskAmount >= 0 && propSelected.StatusForWager is 10 or 9)
                            {
                                var betResult = CreateStraightWager(propSelected, game.FixtureId, Betslip.IdPlayer, game.HomeTeam, game.VisitorTeam, game.SportName);

                                propSelected.StatusForWager = betResult.StatusForWager;
                                propSelected.BsBetResult = betResult.BsBetResult;
                                propSelected.BsTicketNumber = betResult.BsTicketNumber;

                            }
                        }
                    }
                }
            }

       

            //var MinRiskAmount = (decimal)5;
            //var MaxRiskAmount = (decimal)1000;
            //var MaxWinAmount  = (decimal)2000;


            var MinRiskAmount = _appConfig.MinRiskAmount;
            var MaxRiskAmount = _appConfig.MaxRiskAmount;
            var MaxWinAmount = _appConfig.MaxWinAmount;


            var PlayerLimitsParlay = GetPlayerLimitsParlay(Betslip.IdPlayer);
            var AgentLimitsParlay = GetAgentLimitsParlay(Betslip.IdPlayer);

            if (PlayerLimitsParlay != null)
            {
                MinRiskAmount = PlayerLimitsParlay.MinWager;
                MaxRiskAmount = PlayerLimitsParlay.MaxWager;
                MaxWinAmount = PlayerLimitsParlay.MaxPayout;
            }
            else if(AgentLimitsParlay != null)
            {
                MinRiskAmount = AgentLimitsParlay.MinWager;
                MaxRiskAmount = AgentLimitsParlay.MaxWager;
                MaxWinAmount = AgentLimitsParlay.MaxPayout;
            }


            if (validForParlay && Betslip.ParlayRiskAmount > 0 && Betslip.ParlayRiskAmount >= MinRiskAmount)
            {

                //var dailyTotal = GetTodayWinAmount(Betslip.IdPlayer);

                if (Betslip.ParlayRiskAmount >= MaxRiskAmount)
                {
                    Betslip.ParlayBetResult = -50;
                    Betslip.ParlayMessage = $"Ticket exceeds your max Risk amount { MaxRiskAmount }";
                }
                else if (Betslip.ParlayWinAmount >= MaxWinAmount)
                {
                    Betslip.ParlayBetResult = -50;
                    Betslip.ParlayMessage = $"Ticket exceededs your max Win amount { MaxWinAmount }";
                }
                else
                {
                    //if (dailyTotal?.WinAmount >= 1500)
                    //{
                    //    Betslip.ParlayBetResult = -50;
                    //    Betslip.ParlayMessage = $"Ticket exceeds your Daily Total Win amount. { 1500 }";
                    //}
                    //else if (dailyTotal?.RiskAmount >= 11000)
                    //{
                    //    Betslip.ParlayBetResult = -50;
                    //    Betslip.ParlayMessage = $"Ticket exceeds your Daily Total Risk amount. {}";
                    //}
                        
                    //else {
                        var obj = CreateParlayWager(Betslip);
                        Betslip.ParlayBetResult = obj.ParlayBetResult;
                        Betslip.ParlayBetTicket = obj.ParlayBetTicket;
                  //  }
                }
            }
            else if (validForParlay) {
                Betslip.ParlayBetResult = -50;
                Betslip.ParlayMessage = "";
            }
            else
            {
                Betslip.ParlayBetResult = -60;
                Betslip.ParlayMessage = "";
            }
            return Betslip;
        }

        public decimal ParlayCalculateWin(LSport_BetSlipObj Betslip)
        {
            float factor = GeParlayFactor(Betslip);

            return Math.Round(Convert.ToDecimal(factor * Betslip.ParlayRiskAmount), MidpointRounding.AwayFromZero) - Betslip.ParlayRiskAmount;
        }

        public float GeParlayFactor(LSport_BetSlipObj Betslip)
        {
            float factor = 1;

            try
            {
                foreach (var game in Betslip.Events)
                {
                    foreach (var sel in game.Selections)
                    {
                        float originalOdd = (float)sel.Odds1;

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
                if (odd < 0) //negativo
                {
                    win = -1 * ((100 * risk) / odd);
                }
                else
                {
                    win = (odd * risk) / 100;
                }
            }
            catch (Exception)
            {

                return 0;
            }

            return win;
        }

        public LSport_EventPropDto validateProp(LSport_EventPropDto originalProp, Bet LSportBetLine,  bool acceptLineChanged, int fixtureId, int idplayer)
        {
            try
            {
                //var limits = GetPlayerLimits(idplayer);
                //var MinRiskAmount = (decimal)10;//500   **estos son los oldsvalues**
                //var MaxRiskAmount = (decimal)1000;//100
                //var MaxWinAmount = (decimal)2000;//100


                var MinRiskAmount = _appConfig.MinRiskAmount; 
                var MaxRiskAmount = _appConfig.MaxRiskAmount;
                var MaxWinAmount = _appConfig.MaxWinAmount;


                var PlayerLimitsStraight = GetPlayerLimitsStraight(idplayer, fixtureId);
                var AgentLimitsStraight = GetAgentLimitsStraight(idplayer, fixtureId);
                if (PlayerLimitsStraight != null)
                {
                    MinRiskAmount = PlayerLimitsStraight.MinWager;
                    MaxRiskAmount = PlayerLimitsStraight.MaxWager;
                    MaxWinAmount = PlayerLimitsStraight.MaxPayout;
                }
                else if (AgentLimitsStraight != null)
                {
                    MinRiskAmount = AgentLimitsStraight.MinWager;
                    MaxRiskAmount = AgentLimitsStraight.MaxWager;
                    MaxWinAmount = AgentLimitsStraight.MaxPayout;
                }

                if (!GetPlayerInfo(idplayer).Access)
                {
                    originalProp.StatusForWager = 5; //linea cambio y player no acepta cambio de linea
                    originalProp.BsBetResult = -50;
                    originalProp.BsMessage = "Contact your Agent.";
                }
                else 
                {
                    if (LSportBetLine != null && LSportBetLine.Status == 1 /*Line Open*/)
                    {
                        if (int.Parse(LSportBetLine.PriceUS) == originalProp.Odds1)
                        {
                            originalProp.StatusForWager = 10;  //ready for wager
                        }
                        else
                        {
                            if (acceptLineChanged)
                            {
                                originalProp.StatusForWager = 9;  //Line changed but player accept line changes
                                originalProp.Odds1 = int.Parse(LSportBetLine.PriceUS);
                                originalProp.Price = Convert.ToDecimal(LSportBetLine.Price);
                                originalProp.Line1 = LSportBetLine.Line;
                                originalProp.BaseLine = LSportBetLine.BaseLine;
                            }
                            else
                            {
                                originalProp.StatusForWager = 5; //linea cambio y player no acepta cambio de linea
                                originalProp.BsBetResult = -50;
                                originalProp.BsMessage = "Line Changed, please check the new value";
                            }
                        }
                    }
                    else
                    {
                        //linea cerrada
                        originalProp.StatusForWager = 5; //linea cambio y player no acepta cambio de linea
                        originalProp.BsBetResult = -50;
                        originalProp.BsMessage = "Line is closed";
                    }

                    if (originalProp.StatusForWager is 10 or 9 && originalProp.BsRiskAmount > 0)
                    {
                        //var dailyTotal = GetTodayWinAmount(idplayer);
                        //if (dailyTotal?.WinAmount >= 1500 || dailyTotal?.RiskAmount >= 11000)
                        //{
                        //    originalProp.StatusForWager = 5;
                        //    originalProp.BsBetResult = -50;
                        //    originalProp.BsMessage = "Exceeded Daily Total Win amount.";
                        //}
                        //else {
                            if (originalProp.Odds1 < 0 && originalProp.BsRiskAmount > 0)
                            {
                                if (originalProp.BsWinAmount < MinRiskAmount)
                                {
                                    originalProp.StatusForWager = 5; 
                                    originalProp.BsBetResult = -50;
                                    originalProp.BsMessage = $"Less than min risk amount. Min = {MinRiskAmount}";
                                }
                                else if (originalProp.BsWinAmount > MaxRiskAmount)
                                {
                                    originalProp.StatusForWager = 5; 
                                    originalProp.BsBetResult = -50;
                                    originalProp.BsMessage = $"Exceeded max risk amount. Max = {MaxRiskAmount}";
                                }
                                else if (originalProp.BsWinAmount > MaxWinAmount)
                                {
                                    originalProp.StatusForWager = 5; 
                                    originalProp.BsBetResult = -50;
                                    originalProp.BsMessage = $"Exceeded max win amount. Max = { MaxWinAmount}";
                                }
                            }
                            else if (originalProp.BsRiskAmount > 0)
                            {
                                if (originalProp.BsRiskAmount < MinRiskAmount)
                                {
                                    originalProp.StatusForWager = 5; 
                                    originalProp.BsBetResult = -50;
                                    originalProp.BsMessage = $"Less than min risk amount. Min = { MinRiskAmount }";
                                }
                                else if (originalProp.BsRiskAmount > MaxRiskAmount)
                                {
                                    originalProp.StatusForWager = 5; 
                                    originalProp.BsBetResult = -50;
                                    originalProp.BsMessage = $"Exceeded max risk amount. Max = {MaxRiskAmount}";
                                }
                                else if (originalProp.BsRiskAmount > MaxWinAmount)
                                {
                                    originalProp.StatusForWager = 5; 
                                    originalProp.BsBetResult = -50;
                                    originalProp.BsMessage = $"Exceeded max win amount. Max = {MaxWinAmount}";
                                }
                            }
                        //}
                    }
                    //else
                    //{
                    //    originalProp.StatusForWager = 5; //linea cambio y player no acepta cambio de linea
                    //    originalProp.BsBetResult = -50;
                    //    originalProp.BsMessage = "Risk amount must be greater than 0.";
                    //}

                }


            }
            catch (Exception ex)
            {
                //linea cerrada
                originalProp.StatusForWager = 5;
                originalProp.BsBetResult = -50;
                originalProp.BsMessage = "Error trying to push the bet";
            }
            return originalProp;
        }

        private PlayerLimitsHierarchyParlay GetPlayerLimitsParlay(int PlayerId)
        {
            var oPlayerHierarchy = GetPlayerHierarchy(PlayerId);
            LiveDbClass oLiveClass = new LiveDbClass(moverConnString);
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
                MaxPayout = 0
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
            LiveDbClass oLiveClass = new LiveDbClass(moverConnString);
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
                MaxPayout = 0
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

        private AgentLimitsHierarchyStraight GetAgentLimitsStraight(int PlayerId, int FixtureId)
        {
            var oPlayerHierarchy = GetPlayerHierarchy(PlayerId);
            LiveDbClass oLiveClass = new LiveDbClass(moverConnString);
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
                MaxPayout = 0
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
            LiveDbClass oLiveClass = new LiveDbClass(moverConnString);
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
                MaxPayout = 0
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
                            MaxPayout = oLimitsSportMaster.MaxPayout
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

        public LSport_EventPropDto CreateStraightWager(LSport_EventPropDto propSelected, int fixtureId, int idPlayer, string homeTeam, string visitorTeam, string sportName)
        {
            try
            {
                //var PlayerData = GetPlayerData(idPlayer);

                int winAmount = (int)propSelected.BsWinAmount;
                int riskAmount = (int)propSelected.BsRiskAmount;

                if (propSelected.StatusForWager == 9)
                { //linea cambio, igual para win and risk

                    // si oods es negativo afectamos el risk
                    // si odds es positivo afectamos el win
                    winAmount = (int)StraightCalculateWin((int)propSelected.Odds1, (int)propSelected.BsRiskAmount);
                }

                propSelected.BsRiskAmount = riskAmount;
                propSelected.BsWinAmount = winAmount;

                //insertamos el straight en las tablas auxiliares

                int idlivewager = InsertLiveWagerHeader(idPlayer, 1, (int)riskAmount, (int)winAmount, "Straight", "10.1.1.1", 1, 0);

                if (idlivewager > 0)
                {
                    string Description /*255*/ = "VegasLive #" + idlivewager + " [" + fixtureId + "] " + sportName + " / " + visitorTeam + " @ " + homeTeam;

                    string CompleteDescription/*100*/ = $"{visitorTeam} @ {homeTeam} {propSelected.MarketName} • {propSelected.Name} • {propSelected.BaseLine} • {propSelected.Odds1}";

                   var idlivewagerDetail = InsertLiveWagerDetail(idlivewager, fixtureId, propSelected.MarketId, propSelected.IdL1, propSelected.BaseLine, propSelected.Line1, (int)propSelected.Odds1, (decimal)propSelected.Price, propSelected.OriginalName, CompleteDescription, (int)riskAmount, (int)winAmount);

                   
                    if(idlivewagerDetail > 0)
                    {

                        var idDgsWager = InsertDgsWagerHeader(idPlayer, riskAmount, winAmount, Description, Description, "10.0.0.0");

                        if (idDgsWager > 0)
                        {
                            InsertDgsWagerDetail(idDgsWager, CompleteDescription, CompleteDescription);
                        }

                        //actualizamos el idwager en la tabla auxiliar
                        UpdateLiveWagerHeader(idlivewager, idDgsWager);

                        propSelected.BsTicketNumber = idlivewager + "-" + idDgsWager;
                        propSelected.BsBetResult = 1000; //success

                    }
                    else
                    {
                        UpdateLiveWagerHeader(idlivewager, -100);
                        propSelected.BsBetResult = -1;
                    }
                }
                else
                {
                    propSelected.BsBetResult = -1;
                }

            }
            catch (Exception ex)
            {

            }
            return propSelected;
        }

        public LSport_BetSlipObj CreateParlayWager(LSport_BetSlipObj betslipObj)
        {
            try
            {
                List<int> ListDetailWager = new List<int>();

                var descriptionList = new List<string>();

                var PlayerData = GetPlayerData(betslipObj.IdPlayer);

                int winAmount = (int)ParlayCalculateWin(betslipObj);
                int riskAmount = (int)betslipObj.ParlayRiskAmount;

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

                int idlivewager = InsertLiveWagerHeader(betslipObj.IdPlayer, 1, (int)riskAmount, (int)winAmount, "Parlay", "10.1.1.1", numberEvents, 1);

                if (idlivewager > 0)
                {
                    string sports = " (";
                    foreach (var item in betslipObj.Events)
                    {
                        if (item.Selections.Any())
                            sports += " • " + item.SportName;
                    }
                    sports += ")";

                    string Description = "VegasLive #" + idlivewager + " PARLAY " + numberEvents + " TEAMS";
 
                    foreach (var item in betslipObj.Events)
                    {
                        foreach (var sel in item.Selections)
                        {
                            string itemDescription = "[" + item.FixtureId + "] " + item.SportName + " • " + item.VisitorTeam + "@" + item.HomeTeam + " • " + sel.MarketName + " • " + sel.Name + " • " + sel.BaseLine + " • " + sel.Odds1;
                            descriptionList.Add(itemDescription);

                            ListDetailWager.Add(InsertLiveWagerDetail(idlivewager, item.FixtureId, sel.MarketId, sel.IdL1, sel.BaseLine, sel.Line1, (int)sel.Odds1, (decimal)sel.Price, sel.OriginalName, itemDescription, (int)riskAmount, (int)winAmount));
                        }
                    }

                    //revisamos que los detailles y el header se hayan ingresado correctamente


                    if (ListDetailWager.Count == numberEvents && ListDetailWager.All(x => x > 1))
                    {

                        var idDgsWager = InsertDgsWagerHeader(betslipObj.IdPlayer, riskAmount, winAmount, Description, Description, "10.0.0.0");

                        if (idDgsWager > 0)
                        {
                            foreach (var item in descriptionList)
                            {
                                InsertDgsWagerDetail(idDgsWager, item, item);
                            }
                        }
                        else
                        {
                            UpdateLiveWagerHeader(idlivewager, -100);
                            betslipObj.ParlayBetResult = -1;
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
                        UpdateLiveWagerHeader(idlivewager, -100);
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
                using var connection = new SqlConnection(dgsConnString);

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
                using var connection = new SqlConnection(dgsConnString);

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
                using var connection = new SqlConnection(dgsConnString);

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
                using var connection = new SqlConnection(moverConnString);
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
                    using var connection = new SqlConnection(dgsConnString);
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
                using var connection = new SqlConnection(dgsConnString);
                idWt = connection.Query<Int32>("select IdWagerType from WAGERTYPE where IdProfile= @IdProfile and WagerType= @wagerType", new { IdProfile, WagerType }).FirstOrDefault();
            }
            catch (Exception ex)
            {
            }
            return idWt;
        }

        public int InsertDgsWagerHeader(int IdPlayer, int RiskAmount, int OriginalWinAmount, string CompleteDescription/*100*/, string Description/*255*/, string Ip)
        {
            int resp = 0;
            try
            {
                using (var connection = new SqlConnection(dgsConnString))
                {
                    var procedure = "[VegasLive_InsertBet]";
                    var values = new
                    {
                        IdPlayer = IdPlayer,
                        RiskAmount = RiskAmount,
                        OriginalWinAmount = OriginalWinAmount,
                        CompleteDescription = CompleteDescription.Length >= 100 ? CompleteDescription.Substring(0, 99) : CompleteDescription,
                        Description = Description,
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
                using (var connection = new SqlConnection(dgsConnString))
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
                using var connection = new SqlConnection(moverConnString);
                connection.Query<string>(@"Update tb_MGL_WagerHeader SET DgsIdWager = " + dgsIdWager + "  WHERE IdLiveWager = " + idLiveWager).FirstOrDefault();
            }
            catch (Exception ex)
            {
            }
            return idWt;
        }

        public int InsertLiveWagerHeader(int IdPlayer, int IdWagerType, decimal RiskAmount, decimal WinAmount, string Description, string Ip, int NumDetails, Int64 DgsIdWager)
        {
            int resp = 0;
            try
            {
                using (var connection = new SqlConnection(moverConnString))
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
                        DgsIdWager = DgsIdWager
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
                using (var connection = new SqlConnection(moverConnString))
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

        public PlayerInfoDto GetPlayerInfo(int idplayer)
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
                    else
                    {
                        using (var connection = new SqlConnection(dgsConnString))
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
                using var connection = new SqlConnection(moverConnString);
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
                using var connection = new SqlConnection(moverConnString);
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
                using var connection = new SqlConnection(dgsConnString);
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
                    var playerdetails = GetPlayerInfoForLiveWagers(wager.IdPlayer);

                    wager.Player = playerdetails.Player;
                    wager.Agent = playerdetails.Agent;
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
                using (var connection = new SqlConnection(moverConnString))
                {
                    wagers = connection.Query<MoverWagerHeaderDto>(@"SELECT IdLiveWager, IdPlayer, PlacedDateTime, IdWagerType, RiskAmount, WinAmount, Description, Result, Graded, Ip, NumDetails, DgsIdWager FROM tb_MGL_WagerHeader WHERE GRADED = 0 and DgsIdWager <> -1").ToList();
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
                using (var connection = new SqlConnection(moverConnString))
                {
                    wagers = connection.Query<MoverWagerDetailDto>(@"SELECT IdLiveWagerDetail, IdLiveWager, FixtureId, MarketId, LineId, BaseLine, Line, Odds, Price, PickTeam, Result, CompleteDescription, RiskAmount, WinAmount FROM TB_MGL_WAGERDETAIL WHERE IDLIVEWAGER=" + idLiveWager).ToList();
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
                using (var connection = new SqlConnection(dgsConnString))
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

        public int UpdateWagerDetailResult(int IdLiveWagerDetail, int IdLiveWager, int Result, int IdUser)
        { 
            //TODO revisar si el idUser es valido!
            int rest = 0;
            try
            {
                using (var connection = new SqlConnection(moverConnString))
                {
                    var res = connection.Query<string>(@"UPDATE TB_MGL_WAGERDETAIL SET RESULT = " + Result + ", IdUserManualGrade = " + IdUser + " WHERE IDLIVEWAGERDETAIL = " + IdLiveWagerDetail + " AND IDLIVEWAGER = " + IdLiveWager).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                rest = -1;
              //  _ = new Misc().WriteErrorLog("MoverDbClass", "UpdateWagerDetailResult", ex.Message, ex.StackTrace);
            }
            //return pickId;
            return rest;
        }//end method

        public List<CheckListLines> CheckLinesAlive(List<CheckListLines> CheckList)
        {
            List<BetCheck> betList = new List<BetCheck>();

            List<int> FixtureList = CheckList.Select(x => x.FixtureId).Distinct().ToList();


            string result = "";

           // int MarketId = 52;
            //   object BetId = 171905384311502906;

          //  List<int> fix = new List<int>();

         //   fix.Add(11481780);

            var obj = new RestApiClass().CallLSportAPI(FixtureList, "1245", "administracion@corporacionzircon.com", "J83@d784cE");


            if (obj != null)
            {
                if (obj.Body != null && obj.Body.Count > 0)
                {
                    foreach (var body in obj.Body)
                    {
                        if(body != null)
                        {
                            foreach (var item in CheckList)
                            {
                                if (item.FixtureId == body.FixtureId)
                                {
                                    var betMarket = body.Markets != null ? body.Markets.Where(x => x.Id == item.MarketId).FirstOrDefault() : null;

                                    if (betMarket != null && betMarket.Bets != null && betMarket.Bets.Count() > 0)
                                    {
                                        item.BetInfo = betMarket.Bets.Where(x => x.Id.ToString() == item.BetId.ToString()).FirstOrDefault();
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

            //********************************************************************************
            //********************************************************************************
            /*
            if (obj != null)
            {
                if (obj.Body != null && obj.Body.Count > 0)
                {
                    if (obj.Body[0].Fixture != null)
                    {
                        if (obj.Body[0].Fixture.Status == 2) //juego sigue activo
                        {
                            //ahora revisamos la linea
                            if (obj.Body[0].Markets != null && obj.Body[0].Markets.Count() > 0)
                            {
                                var betMarket = obj.Body[0].Markets.Where(x => x.Id == MarketId).FirstOrDefault();

                                if (betMarket != null && betMarket.Bets != null && betMarket.Bets.Count() > 0)
                                {

                                    var betLine = betMarket.Bets.Where(x => x.Id.ToString() == BetId.ToString()).FirstOrDefault();

                                    if (betLine != null)
                                    {
                                        if (betLine.Status != null && betLine.Status == 1)
                                        {
                                            result = "Encontrada y bien";
                                        }
                                        else
                                        {
                                            result = "BetLine Closed";
                                        }
                                    }
                                    else
                                    {
                                        result = "Betline does not exist";
                                    }
                                }
                                else
                                {
                                    result = "Market closed or It does not exist";
                                }
                            }
                            else
                            {
                                result = "No Markets available";
                            }
                        }
                        else
                        {
                            result = "Game Status closed";
                        }
                    }
                    else
                    {
                        result = "No Fixture ";
                    }
                }
                else
                {
                    result = "No Body";
                }
            }
            else
            {
                result = "No Data";
            }
            */
            return CheckList;

        }//end test



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
