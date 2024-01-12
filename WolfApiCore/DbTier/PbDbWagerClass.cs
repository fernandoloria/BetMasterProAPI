using Dapper;
using Microsoft.Data.SqlClient;
using static WolfApiCore.Models.AdminModels;
using System.Data;
using WolfApiCore.Models;
using static WolfApiCore.Models.LsportsSports;
using System.Security.Cryptography.X509Certificates;

namespace WolfApiCore.DbTier
{
    public class PbDbWagerClass
    {
        private readonly string dgsConnString = "Data Source=192.168.11.29;Initial Catalog=DGSDATA;Persist Security Info=True;User ID=Payments;Password=p@yM3nts2701;TrustServerCertificate=True";
        private readonly string moverConnString = "Data Source=192.168.11.29;Initial Catalog=mover;Persist Security Info=True;User ID=live;Password=d_Ez*gIb8v7NogU;TrustServerCertificate=True";

        public LSport_BetSlipObj ValidateSelectionsForWagers(LSport_BetSlipObj Betslip)
        {
            var validForParlay = true;

            // if (Betslip.IdPlayer != 300563)
            //     return Betslip;


            //validamos todos
            foreach (var Game in Betslip.Events)
            {
                foreach (var propSelected in Game.Selections)
                {
                    var dbProp = validateProp(propSelected, Betslip.AcceptLineChange, Game.FixtureId, Betslip.IdPlayer);
                    propSelected.Odds1 = dbProp.Odds1;
                    propSelected.IdL1 = dbProp.IdL1;
                    propSelected.BaseLine = dbProp.BaseLine;
                    propSelected.BsMessage = dbProp.BsMessage;
                    propSelected.BsBetResult = dbProp.BsBetResult;
                    if (dbProp.StatusForWager != 9 && dbProp.StatusForWager != 10)
                        validForParlay = false;
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

            var MinRiskAmount = (decimal)5;
            var MaxRiskAmount = (decimal)100;
            var MaxWinAmount = (decimal)250;

            var PlayerLimitsParlay = GetPlayerLimitsParlay(Betslip.IdPlayer);
            var AgentLimitsParlay = GetAgentLimitsParlay(Betslip.IdPlayer);
            if (PlayerLimitsParlay != null)
            {
                MinRiskAmount = PlayerLimitsParlay.MinWager;
                MaxRiskAmount = PlayerLimitsParlay.MaxWager;
                MaxWinAmount = PlayerLimitsParlay.MaxPayout;
            }
            else if (AgentLimitsParlay != null)
            {
                MinRiskAmount = AgentLimitsParlay.MinWager;
                MaxRiskAmount = AgentLimitsParlay.MaxWager;
                MaxWinAmount = AgentLimitsParlay.MaxPayout;
            }

            if (validForParlay && Betslip.ParlayRiskAmount > 0 && Betslip.ParlayRiskAmount >= MinRiskAmount)
            {

                var dailyTotal = GetTodayWinAmount(Betslip.IdPlayer);

                if (Betslip.ParlayRiskAmount >= MaxRiskAmount)
                {
                    Betslip.ParlayBetResult = -50;
                    Betslip.ParlayMessage = "Exceeded max risk amount";
                }
                else if (Betslip.ParlayWinAmount >= MaxWinAmount)
                {
                    Betslip.ParlayBetResult = -50;
                    Betslip.ParlayMessage = "Exceeded max win amount";
                }
                else
                {
                    if (dailyTotal?.WinAmount >= 500 || dailyTotal?.RiskAmount >= 1000)
                    {
                        Betslip.ParlayBetResult = -50;
                        Betslip.ParlayMessage = "Exceeded Daily Total Win amount.";
                    }
                    else
                    {
                        var obj = CreateParlayWager(Betslip);
                        Betslip.ParlayBetResult = obj.ParlayBetResult;
                        Betslip.ParlayBetTicket = obj.ParlayBetTicket;
                    }
                }
            }
            else
            {
                Betslip.ParlayBetResult = -50;
                Betslip.ParlayMessage = "Less than min risk amount or negative amount";
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

        public LSport_EventPropDto validateProp(LSport_EventPropDto originalProp, bool acceptLineChanged, int fixtureId, int idplayer)
        {
            try
            {
                //var limits = GetPlayerLimits(idplayer);
                var MinRiskAmount = (decimal)500;
                var MaxRiskAmount = (decimal)100;
                var MaxWinAmount = (decimal)100;

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

                var dbProp = GetPropValues(fixtureId, originalProp.IdL1);

                if (!GetPlayerInfo(idplayer).Access)
                {
                    originalProp.StatusForWager = 5; //linea cambio y player no acepta cambio de linea
                    originalProp.BsBetResult = -50;
                    originalProp.BsMessage = "Contact your Agent.";
                }
                else
                {
                    if (dbProp != null)
                    {
                        if (int.Parse(dbProp.PriceUS) == originalProp.Odds1)
                        {
                            originalProp.StatusForWager = 10;  //ready for wager
                        }
                        else
                        {
                            if (acceptLineChanged)
                            {
                                originalProp.StatusForWager = 9;  //Line changed but player accept line changes
                                originalProp.Odds1 = int.Parse(dbProp.PriceUS);
                                originalProp.Price = Convert.ToDecimal(dbProp.Price);
                                originalProp.Line1 = dbProp.Line;
                                originalProp.BaseLine = dbProp.BaseLine;
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
                        var dailyTotal = GetTodayWinAmount(idplayer);
                        if (dailyTotal?.WinAmount >= 500 || dailyTotal?.RiskAmount >= 1000)
                        {
                            originalProp.StatusForWager = 5;
                            originalProp.BsBetResult = -50;
                            originalProp.BsMessage = "Exceeded Daily Total Win amount.";
                        }
                        else
                        {
                            if (originalProp.Odds1 < 0 && originalProp.BsRiskAmount > 0)
                            {
                                if (originalProp.BsWinAmount > MaxWinAmount)
                                {
                                    originalProp.StatusForWager = 5; //linea cambio y player no acepta cambio de linea
                                    originalProp.BsBetResult = -50;
                                    originalProp.BsMessage = "Exceeded max win amount.";
                                }
                                else if (originalProp.BsWinAmount > MaxRiskAmount)
                                {
                                    originalProp.StatusForWager = 5; //linea cambio y player no acepta cambio de linea
                                    originalProp.BsBetResult = -50;
                                    originalProp.BsMessage = "Exceeded max risk amount.";
                                }
                                else if (originalProp.BsWinAmount < MinRiskAmount)
                                {
                                    originalProp.StatusForWager = 5; //linea cambio y player no acepta cambio de linea
                                    originalProp.BsBetResult = -50;
                                    originalProp.BsMessage = "Less than min risk amount.";
                                }
                            }
                            else if(originalProp.BsRiskAmount > 0)
                            {
                                if (originalProp.BsRiskAmount < MinRiskAmount)
                                {
                                    originalProp.StatusForWager = 5; //linea cambio y player no acepta cambio de linea
                                    originalProp.BsBetResult = -50;
                                    originalProp.BsMessage = "Less than min risk amount.";
                                }
                                else if (originalProp.BsRiskAmount > MaxRiskAmount)
                                {
                                    originalProp.StatusForWager = 5; //linea cambio y player no acepta cambio de linea
                                    originalProp.BsBetResult = -50;
                                    originalProp.BsMessage = "Exceeded max risk amount.";
                                }
                                else if (originalProp.BsWinAmount > MaxWinAmount)
                                {
                                    originalProp.StatusForWager = 5; //linea cambio y player no acepta cambio de linea
                                    originalProp.BsBetResult = -50;
                                    originalProp.BsMessage = "Exceeded max win amount.";
                                }
                            }
                        }
                    }
                    //else if 
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
                    string CompleteDescription = "VPb #" + idlivewager;
                    string DetailCompleteDescription = "[" + idlivewager + "]" + "[" + fixtureId + "] " + sportName + " • " + visitorTeam + "@" + homeTeam + " • " + propSelected.MarketName + " • " + propSelected.Name + " • " + propSelected.BaseLine + " • " + propSelected.Odds1;

                    int idliveDetail = InsertLiveWagerDetail(idlivewager, fixtureId, propSelected.MarketId, propSelected.IdL1, propSelected.BaseLine, propSelected.Line1, (int)propSelected.Odds1, (decimal)propSelected.Price, propSelected.Name, DetailCompleteDescription, (int)riskAmount, (int)winAmount);

                    var idDgsWager = InsertDgsWager(idPlayer, riskAmount, winAmount, CompleteDescription, DetailCompleteDescription, "10.0.0.0");

                    //actualizamos el idwager en la tabla auxiliar
                    UpdateLiveWagerHeader(idlivewager, idDgsWager);

                    propSelected.BsTicketNumber = idlivewager + "-" + idDgsWager;
                    propSelected.BsBetResult = 1000; //success
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
                var PlayerData = GetPlayerData(betslipObj.IdPlayer);

                int winAmount = (int)ParlayCalculateWin(betslipObj);
                int riskAmount = (int)betslipObj.ParlayRiskAmount;

                betslipObj.ParlayRiskAmount = riskAmount;
                betslipObj.ParlayWinAmount = winAmount;

                //insertamos el straight en las tablas auxiliares

                int idlivewager = InsertLiveWagerHeader(betslipObj.IdPlayer, 1, (int)riskAmount, (int)winAmount, "Parlay", "10.1.1.1", betslipObj.Events.Count(), 1);

                if (idlivewager > 0)
                {
                    string sports = " (";
                    foreach (var item in betslipObj.Events)
                    {
                        sports += " • " + item.SportName;
                    }
                    sports += ")";

                    string ParlayDescription = "PARLAY " + betslipObj.Events.Count() + " TEAMS";
                    string CompleteDescription = "VPB #" + idlivewager;
                    string DetailCompleteDescription = "[" + idlivewager + "]" + "[" + riskAmount + "/" + winAmount + "] " + ParlayDescription + sports;


                    foreach (var item in betslipObj.Events)
                    {
                        foreach (var sel in item.Selections)
                        {
                            string itemDescription = "[" + idlivewager + "]" + "[" + item.FixtureId + "] " + item.SportName + " • " + item.VisitorTeam + "@" + item.HomeTeam + " • " + sel.MarketName + " • " + sel.Name + " • " + sel.BaseLine + " • " + sel.Odds1;

                            InsertLiveWagerDetail(idlivewager, item.FixtureId, sel.MarketId, sel.IdL1, sel.BaseLine, sel.Line1, (int)sel.Odds1, (decimal)sel.Price, sel.OriginalName, itemDescription, (int)riskAmount, (int)winAmount);
                        }
                    }

                    var idDgsWager = InsertDgsWager(betslipObj.IdPlayer, riskAmount, winAmount, CompleteDescription, DetailCompleteDescription, "10.0.0.0");

                    //actualizamos el idwager en la tabla auxiliar
                    UpdateLiveWagerHeader(idlivewager, idDgsWager);

                    //  propSelected.BsTicketNumber = idlivewager + "-" + idDgsWager;
                    //  propSelected.BsBetResult = 0;
                    betslipObj.ParlayBetResult = 0;
                    betslipObj.ParlayBetTicket = idlivewager + "-" + idDgsWager;
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
                PropValue = connection.Query<CompletePropMarket>("SELECT Id, FixtureId, MarketId, MarketName, MainLine, Name, Status, StartPrice, Price, PriceUS, BaseLine, Line FROM tb_MGL_FixturePrematchBetLines where Fixtureid = @fixtureId and status = 1 and id = @Id",
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

        public int InsertDgsWager(int IdPlayer, int RiskAmount, int OriginalWinAmount, string CompleteDescription, string DetailCompleteDescription, string Ip)
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
                        CompleteDescription = CompleteDescription,
                        DetailCompleteDescription = DetailCompleteDescription,
                        IPAddress = Ip,
                        IdSport = "VPB"
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
                if (idplayer > 0)
                {
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
                            if (resp != null)
                            {
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

    }//end class

}//end namespace
