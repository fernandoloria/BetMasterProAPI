using Microsoft.Data.SqlClient;
using Dapper;
using System.Data;
using static WolfApiCore.Models.AdminModels;
using WolfApiCore.Models;
using WolfApiCore.Utilities;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace WolfApiCore.DbTier
{
    public class LiveAdminDbClass
    {
        private readonly string dgsConnString = "Data Source=192.168.11.29;Initial Catalog=DGSDATA;Persist Security Info=True;User ID=Payments;Password=p@yM3nts2701;TrustServerCertificate=True";
        private readonly string moverConnString = "Data Source=192.168.11.29;Initial Catalog=mover;Persist Security Info=True;User ID=live;Password=d_Ez*gIb8v7NogU;TrustServerCertificate=True";

        public AgentLoginResp AdminLogin(AgentLoginReq LoginReq)
        {
            AgentLoginResp agentLoginResp = null;
            try
            {
                if (LoginReq != null)
                {
                    if (LoginReq.Origin == 1) //usuario de DGS
                    {
                        string sql = "exec TNTMakerLogin @User, @Password";
                        var values = new { User = LoginReq.Username, LoginReq.Password };
                        using var connection = new SqlConnection(dgsConnString);
                        agentLoginResp = connection.Query<AgentLoginResp>(sql, values).FirstOrDefault();
                    }
                    else if (LoginReq.Origin == 2) //Agent Site
                    {
                        string sql = "exec sp_MGL_AdminLogin @Agent, @Password";
                        var values = new { Agent = LoginReq.Username, LoginReq.Password };
                        using var connection = new SqlConnection(dgsConnString);
                        agentLoginResp = connection.Query<AgentLoginResp>(sql, values).FirstOrDefault();
                    }
                }
            }
            catch (Exception ex)
            {
                _ = ex.Message;
            }
            return agentLoginResp;
        }

        public List<GetPlayerListResp> GetPlayersByIdAgent(GetPlayerListReq req)
        {
            List<GetPlayerListResp> PlayersByIdAgentResp = null;
            string sql = "exec Agent_GetPlayers @prmIdAgent";
            var values = new { prmIdAgent = req.IdAgent };
            try
            {

                using var connection = new SqlConnection(dgsConnString);
                PlayersByIdAgentResp = connection.Query<GetPlayerListResp>(sql, values).ToList();

            }
            catch (Exception ex)
            {
                _ = ex.Message;
            }
            return PlayersByIdAgentResp;
        }

        public List<LimitsRightsVerificationResp> SetProfileLimitsMassive(List<SetProfileLimitsReq> req)
        {
            List<LimitsRightsVerificationResp> resp = new();
            try
            {
                if (req.Count() > 0)
                {
                    foreach (var item in req)
                    {
                        string sql = "exec sp_MGL_SetProfileLimitsMassive @AgentId ,@IdWagerType ,@SportId ,@LeagueId, @SportName, @LeagueName, @FixtureId ,@MaxWager ,@MinWager ,@MaxPayout ,@MinPayout  ,@MinPrice ,@MaxPrice ,@TotAmtGame";
                        var values = new { item.AgentId, item.IdWagerType, item.SportId, item.LeagueId, item.SportName, item.LeagueName, item.FixtureId, item.MaxWager, item.MinWager, item.MaxPayout, item.MinPayout, item.MinPrice, item.MaxPrice, item.TotAmtGame };

                        using var connection = new SqlConnection(moverConnString);
                        List<GetAgentHierarchyResp> HierarchyNodes = GetAgentHierarchy(new GetAgentHierarchyReq() { IdAgent = item.AgentId });
                        LimitsRightsVerificationResp limitsMaster = CheckLimitsRights(item);
                        switch (limitsMaster.Code)
                        {
                            //codigo 14, esta correcto y puede actualizar
                            case 14:
                                foreach (var node in HierarchyNodes)
                                {


                                    if (item.AgentId == node.AgentID)
                                    {
                                        _ = connection.Query<ProfileLimitsResp>(sql, new { node.AgentID, item.IdWagerType, item.SportId, item.LeagueId, item.SportName, item.LeagueName, item.FixtureId, item.MaxWager, item.MinWager, item.MaxPayout, item.MinPayout, item.MinPrice, item.MaxPrice, item.TotAmtGame });
                                        limitsMaster.AgentId = node.AgentID;
                                        resp.Add(new LimitsRightsVerificationResp()
                                        {
                                            AgentId = node.AgentID,
                                            Code = limitsMaster.Code,
                                            Message = limitsMaster.Message,
                                            Applied = true,
                                            MaxWager = item.MaxWager,
                                            MinWager = item.MinWager,
                                            MaxPayout = item.MaxPayout,
                                            MinPrice = item.MinPrice,
                                            MaxPrice = item.MaxPrice,
                                            TotAmtGame = item.TotAmtGame
                                        });
                                    }
                                    else
                                    {

                                        GetProfileLimitsReq reqMaster = new GetProfileLimitsReq()
                                        {
                                            AgentId = node.AgentID,
                                            FixtureId = item.FixtureId,
                                            IdWagerType = item.IdWagerType,
                                            LeagueId = item.LeagueId,
                                            SportId = item.SportId,
                                            LeagueName = item.LeagueName,
                                            SportName = item.SportName,
                                            MaxPayout = item.MaxPayout,
                                            MinPayout = item.MinPayout,
                                            MaxWager = item.MaxWager,
                                            MinWager = item.MinWager,

                                            MinPrice = item.MinPrice,
                                            MaxPrice = item.MaxPrice,
                                            TotAmtGame = item.TotAmtGame,

                                            ModifiedAt = DateTime.Now
                                        };
                                        var ListReq = new List<GetProfileLimitsReq>
                                        {
                                            reqMaster
                                        };
                                        List<ProfileLimitsResp> ProfileLimitsResp = GetProfileLimits(ListReq);

                                        if (ProfileLimitsResp.Count() > 0)
                                        {



                                            _ = connection.Query<ProfileLimitsResp>(sql, new { node.AgentID, item.IdWagerType, item.SportId, item.LeagueId, item.SportName, item.LeagueName, item.FixtureId, item.MaxWager, item.MinWager, item.MaxPayout, item.MinPayout, item.MinPrice, item.MaxPrice, item.TotAmtGame });
                                            limitsMaster.AgentId = node.AgentID;
                                            limitsMaster.Applied = true;
                                            limitsMaster.MaxWager = item.MaxWager;
                                            limitsMaster.MinWager = item.MinWager;
                                            limitsMaster.MaxPayout = item.MaxPayout;

                                            limitsMaster.MinPrice = item.MinPrice;
                                            limitsMaster.MaxPrice = item.MaxPrice;
                                            limitsMaster.TotAmtGame = item.TotAmtGame;

                                            resp.Add(new LimitsRightsVerificationResp()
                                            {
                                                AgentId = node.AgentID,
                                                Code = limitsMaster.Code,
                                                Message = limitsMaster.Message,
                                                Applied = true,
                                                MaxWager = item.MaxWager,
                                                MinWager = item.MinWager,
                                                MaxPayout = item.MaxPayout,
                                                MinPrice = item.MinPrice,
                                                MaxPrice = item.MaxPrice,
                                                TotAmtGame = item.TotAmtGame,
                                            });
                                        }
                                        else
                                        {
                                            _ = connection.Query<ProfileLimitsResp>(sql, new { node.AgentID, item.IdWagerType, item.SportId, item.LeagueId, item.SportName, item.LeagueName, item.FixtureId, item.MaxWager, item.MinWager, item.MaxPayout, item.MinPayout, item.MinPrice, item.MaxPrice, item.TotAmtGame });
                                            limitsMaster.AgentId = node.AgentID;
                                            limitsMaster.Applied = true;
                                            limitsMaster.MaxWager = item.MaxWager;
                                            limitsMaster.MinWager = item.MinWager;
                                            limitsMaster.MaxPayout = item.MaxPayout;

                                            limitsMaster.MinPrice = item.MinPrice;
                                            limitsMaster.MaxPrice = item.MaxPrice;
                                            limitsMaster.TotAmtGame = item.TotAmtGame;

                                            resp.Add(new LimitsRightsVerificationResp()
                                            {
                                                AgentId = node.AgentID,
                                                Code = limitsMaster.Code,
                                                Message = limitsMaster.Message,
                                                Applied = true,
                                                MaxWager = item.MaxWager,
                                                MinWager = item.MinWager,
                                                MaxPayout = item.MaxPayout,
                                                MinPrice = item.MinPrice,
                                                MaxPrice = item.MaxPrice,
                                                TotAmtGame = item.TotAmtGame,
                                            });
                                        }





                                    }
                                }
                                break;
                            case 16:
                                foreach (var node in HierarchyNodes)
                                {


                                    _ = connection.Query<ProfileLimitsResp>(sql, new { node.AgentID, item.IdWagerType, item.SportId, item.LeagueId, item.SportName, item.LeagueName, item.FixtureId, item.MaxWager, item.MinWager, item.MaxPayout, item.MinPayout, item.MinPrice, item.MaxPrice, item.TotAmtGame });
                                    limitsMaster.AgentId = node.AgentID;
                                    limitsMaster.Applied = true;
                                    limitsMaster.MaxWager = item.MaxWager;
                                    limitsMaster.MinWager = item.MinWager;
                                    limitsMaster.MaxPayout = item.MaxPayout;
                                    limitsMaster.MinPrice = item.MinPrice;
                                    limitsMaster.MaxPrice = item.MaxPrice;
                                    limitsMaster.TotAmtGame = item.TotAmtGame;
                                    resp.Add(new LimitsRightsVerificationResp()
                                    {
                                        AgentId = node.AgentID,
                                        Code = limitsMaster.Code,
                                        Message = limitsMaster.Message,
                                        Applied = true,
                                        MaxWager = item.MaxWager,
                                        MinWager = item.MinWager,
                                        MaxPayout = item.MaxPayout,
                                        MinPrice = item.MinPrice,
                                        MaxPrice = item.MaxPrice,
                                        TotAmtGame = item.TotAmtGame,
                                    });
                                }
                                break;
                            case 23:
                                var canupd3 = CanUpdWagerLimits(item.AgentId);

                                if (canupd3)
                                {

                                    _ = connection.Query<ProfileLimitsResp>(sql, values);
                                    limitsMaster.AgentId = item.AgentId;
                                    limitsMaster.Message = "Limits applied, are in of the master agent range.";
                                    limitsMaster.Applied = true;
                                    limitsMaster.MaxWager = item.MaxWager;
                                    limitsMaster.MinWager = item.MinWager;
                                    limitsMaster.MaxPayout = item.MaxPayout;
                                    limitsMaster.MinPrice = item.MinPrice;
                                    limitsMaster.MaxPrice = item.MaxPrice;
                                    limitsMaster.TotAmtGame = item.TotAmtGame;
                                    resp.Add(new LimitsRightsVerificationResp()
                                    {
                                        AgentId = item.AgentId,
                                        Code = 14,
                                        Message = limitsMaster.Message,
                                        Applied = true,
                                        MaxWager = item.MaxWager,
                                        MinWager = item.MinWager,
                                        MaxPayout = item.MaxPayout,
                                        MinPrice = item.MinPrice,
                                        MaxPrice = item.MaxPrice,
                                        TotAmtGame = item.TotAmtGame,
                                    });


                                }
                                else
                                {

                                    if (item.MinWager >= limitsMaster.MinWager && item.MaxWager <= limitsMaster.MaxWager && item.MaxPayout <= limitsMaster.MaxPayout)
                                    {
                                        foreach (var node in HierarchyNodes)
                                        {


                                            if (item.AgentId == node.AgentID)
                                            {
                                                _ = connection.Query<ProfileLimitsResp>(sql, new { node.AgentID, item.IdWagerType, item.SportId, item.LeagueId, item.SportName, item.LeagueName, item.FixtureId, item.MaxWager, item.MinWager, item.MaxPayout, item.MinPayout, item.MinPrice, item.MaxPrice, item.TotAmtGame });
                                                limitsMaster.AgentId = node.AgentID;
                                                resp.Add(new LimitsRightsVerificationResp()
                                                {
                                                    AgentId = node.AgentID,
                                                    Code = limitsMaster.Code,
                                                    Message = limitsMaster.Message,
                                                    Applied = true,
                                                    MaxWager = item.MaxWager,
                                                    MinWager = item.MinWager,
                                                    MaxPayout = item.MaxPayout,
                                                    MinPrice = item.MinPrice,
                                                    MaxPrice = item.MaxPrice,
                                                    TotAmtGame = item.TotAmtGame
                                                });
                                            }
                                            else
                                            {

                                                GetProfileLimitsReq reqMaster = new GetProfileLimitsReq()
                                                {
                                                    AgentId = node.AgentID,
                                                    FixtureId = item.FixtureId,
                                                    IdWagerType = item.IdWagerType,
                                                    LeagueId = item.LeagueId,
                                                    SportId = item.SportId,
                                                    LeagueName = item.LeagueName,
                                                    SportName = item.SportName,
                                                    MaxPayout = item.MaxPayout,
                                                    MinPayout = item.MinPayout,
                                                    MaxWager = item.MaxWager,
                                                    MinWager = item.MinWager,

                                                    MinPrice = item.MinPrice,
                                                    MaxPrice = item.MaxPrice,
                                                    TotAmtGame = item.TotAmtGame,

                                                    ModifiedAt = DateTime.Now
                                                };
                                                var ListReq = new List<GetProfileLimitsReq>
                                        {
                                            reqMaster
                                        };
                                                List<ProfileLimitsResp> ProfileLimitsResp = GetProfileLimits(ListReq);

                                                if (ProfileLimitsResp.Count() > 0)
                                                {



                                                    _ = connection.Query<ProfileLimitsResp>(sql, new { node.AgentID, item.IdWagerType, item.SportId, item.LeagueId, item.SportName, item.LeagueName, item.FixtureId, item.MaxWager, item.MinWager, item.MaxPayout, item.MinPayout, item.MinPrice, item.MaxPrice, item.TotAmtGame });
                                                    limitsMaster.AgentId = node.AgentID;
                                                    limitsMaster.Applied = true;
                                                    limitsMaster.MaxWager = item.MaxWager;
                                                    limitsMaster.MinWager = item.MinWager;
                                                    limitsMaster.MaxPayout = item.MaxPayout;

                                                    limitsMaster.MinPrice = item.MinPrice;
                                                    limitsMaster.MaxPrice = item.MaxPrice;
                                                    limitsMaster.TotAmtGame = item.TotAmtGame;

                                                    resp.Add(new LimitsRightsVerificationResp()
                                                    {
                                                        AgentId = node.AgentID,
                                                        Code = 14,
                                                        Message = "Limits applied",
                                                        Applied = true,
                                                        MaxWager = item.MaxWager,
                                                        MinWager = item.MinWager,
                                                        MaxPayout = item.MaxPayout,
                                                        MinPrice = item.MinPrice,
                                                        MaxPrice = item.MaxPrice,
                                                        TotAmtGame = item.TotAmtGame,
                                                    });
                                                }
                                                else
                                                {
                                                    _ = connection.Query<ProfileLimitsResp>(sql, new { node.AgentID, item.IdWagerType, item.SportId, item.LeagueId, item.SportName, item.LeagueName, item.FixtureId, item.MaxWager, item.MinWager, item.MaxPayout, item.MinPayout, item.MinPrice, item.MaxPrice, item.TotAmtGame });
                                                    limitsMaster.AgentId = node.AgentID;
                                                    limitsMaster.Applied = true;
                                                    limitsMaster.MaxWager = item.MaxWager;
                                                    limitsMaster.MinWager = item.MinWager;
                                                    limitsMaster.MaxPayout = item.MaxPayout;

                                                    limitsMaster.MinPrice = item.MinPrice;
                                                    limitsMaster.MaxPrice = item.MaxPrice;
                                                    limitsMaster.TotAmtGame = item.TotAmtGame;

                                                    resp.Add(new LimitsRightsVerificationResp()
                                                    {
                                                        AgentId = node.AgentID,
                                                        Code = 14,
                                                        Message = "Limits applied",
                                                        Applied = true,
                                                        MaxWager = item.MaxWager,
                                                        MinWager = item.MinWager,
                                                        MaxPayout = item.MaxPayout,
                                                        MinPrice = item.MinPrice,
                                                        MaxPrice = item.MaxPrice,
                                                        TotAmtGame = item.TotAmtGame,
                                                    });
                                                }





                                            }
                                        }
                                    }
                                    else
                                    {
                                        resp.Add(new LimitsRightsVerificationResp()
                                        {
                                            AgentId = item.AgentId,
                                            Code = 66,
                                            Message = "Limited by Master Agent, out of range.",
                                            Applied = limitsMaster.Applied,
                                            MaxWager = item.MaxWager,
                                            MinWager = item.MinWager,
                                            MaxPayout = item.MaxPayout,

                                            MinPrice = item.MinPrice,
                                            MaxPrice = item.MaxPrice,
                                            TotAmtGame = item.TotAmtGame,
                                        });
                                    }


                                }


                                break;
                            case 51:
                            default:
                                resp.Add(new LimitsRightsVerificationResp()
                                {
                                    AgentId = item.AgentId,
                                    Code = limitsMaster.Code,
                                    Message = limitsMaster.Message,
                                    Applied = limitsMaster.Applied,
                                    MaxWager = item.MaxWager,
                                    MinWager = item.MinWager,
                                    MaxPayout = item.MaxPayout,
                                    MinPrice = item.MinPrice,
                                    MaxPrice = item.MaxPrice,
                                    TotAmtGame = item.TotAmtGame,
                                });
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                resp.Add(new LimitsRightsVerificationResp()
                {
                    AgentId = -1,
                    Code = -1,
                    Message = "Catch a exception: " + ex.Message,
                    Applied = false,
                    MaxWager = 0,
                    MinWager = 0,
                    MaxPayout = 0,
                    MinPrice = 0,
                    MaxPrice = 0,
                    TotAmtGame = 0,
                });
            }
            return resp;
        }



        public List<LimitsRightsVerificationResp> SetProfileLimits(List<SetProfileLimitsReq> req)
        {
            List<LimitsRightsVerificationResp> resp = new();
            try
            {
                if (req.Count() > 0)
                {
                    foreach (var item in req)
                    {
                        string sql = "exec sp_MGL_SetProfileLimits @AgentId ,@IdWagerType ,@SportId ,@LeagueId, @SportName, @LeagueName, @FixtureId ,@MaxWager ,@MinWager ,@MaxPayout ,@MinPayout  ,@MinPrice ,@MaxPrice ,@TotAmtGame";
                        var values = new { item.AgentId, item.IdWagerType, item.SportId, item.LeagueId, item.SportName, item.LeagueName, item.FixtureId, item.MaxWager, item.MinWager, item.MaxPayout, item.MinPayout, item.MinPrice, item.MaxPrice, item.TotAmtGame };

                        using var connection = new SqlConnection(moverConnString);
                        List<GetAgentHierarchyResp> HierarchyNodes = GetAgentHierarchy(new GetAgentHierarchyReq() { IdAgent = item.AgentId });
                        LimitsRightsVerificationResp limitsMaster = CheckLimitsRights(item);
                        switch (limitsMaster.Code)
                        {
                            //codigo 14, esta correcto y puede actualizar
                            case 14:
                                foreach (var node in HierarchyNodes)
                                {


                                    if (item.AgentId == node.AgentID)
                                    {
                                        _ = connection.Query<ProfileLimitsResp>(sql, new { node.AgentID, item.IdWagerType, item.SportId, item.LeagueId, item.SportName, item.LeagueName, item.FixtureId, item.MaxWager, item.MinWager, item.MaxPayout, item.MinPayout, item.MinPrice, item.MaxPrice, item.TotAmtGame });
                                        limitsMaster.AgentId = node.AgentID;
                                        resp.Add(new LimitsRightsVerificationResp()
                                        {
                                            AgentId = node.AgentID,
                                            Code = limitsMaster.Code,
                                            Message = limitsMaster.Message,
                                            Applied = true,
                                            MaxWager = item.MaxWager,
                                            MinWager = item.MinWager,
                                            MaxPayout = item.MaxPayout,
                                            MinPrice = item.MinPrice,
                                            MaxPrice = item.MaxPrice,
                                            TotAmtGame = item.TotAmtGame
                                        });
                                    }
                                    else
                                    {

                                        GetProfileLimitsReq reqMaster = new GetProfileLimitsReq()
                                        {
                                            AgentId = node.AgentID,
                                            FixtureId = item.FixtureId,
                                            IdWagerType = item.IdWagerType,
                                            LeagueId = item.LeagueId,
                                            SportId = item.SportId,
                                            LeagueName = item.LeagueName,
                                            SportName = item.SportName,
                                            MaxPayout = item.MaxPayout,
                                            MinPayout = item.MinPayout,
                                            MaxWager = item.MaxWager,
                                            MinWager = item.MinWager,

                                            MinPrice = item.MinPrice,
                                            MaxPrice = item.MaxPrice,
                                            TotAmtGame = item.TotAmtGame,

                                            ModifiedAt = DateTime.Now
                                        };
                                        var ListReq = new List<GetProfileLimitsReq>
                                        {
                                            reqMaster
                                        };
                                        List<ProfileLimitsResp> ProfileLimitsResp = GetProfileLimits(ListReq);

                                            if (ProfileLimitsResp.Count() > 0)
                                            {



                                                _ = connection.Query<ProfileLimitsResp>(sql, new { node.AgentID, item.IdWagerType, item.SportId, item.LeagueId, item.SportName, item.LeagueName, item.FixtureId, item.MaxWager, item.MinWager, item.MaxPayout, item.MinPayout, item.MinPrice, item.MaxPrice, item.TotAmtGame });
                                                limitsMaster.AgentId = node.AgentID;
                                                limitsMaster.Applied = true;
                                                limitsMaster.MaxWager = item.MaxWager;
                                                limitsMaster.MinWager = item.MinWager;
                                                limitsMaster.MaxPayout = item.MaxPayout;

                                                limitsMaster.MinPrice = item.MinPrice;
                                                limitsMaster.MaxPrice = item.MaxPrice;
                                                limitsMaster.TotAmtGame = item.TotAmtGame;

                                                resp.Add(new LimitsRightsVerificationResp()
                                                {
                                                    AgentId = node.AgentID,
                                                    Code = limitsMaster.Code,
                                                    Message = limitsMaster.Message,
                                                    Applied = true,
                                                    MaxWager = item.MaxWager,
                                                    MinWager = item.MinWager,
                                                    MaxPayout = item.MaxPayout,
                                                    MinPrice = item.MinPrice,
                                                    MaxPrice = item.MaxPrice,
                                                    TotAmtGame = item.TotAmtGame,
                                                });
                                            }
                                            else
                                            {
                                                _ = connection.Query<ProfileLimitsResp>(sql, new { node.AgentID, item.IdWagerType, item.SportId, item.LeagueId, item.SportName, item.LeagueName, item.FixtureId, item.MaxWager, item.MinWager, item.MaxPayout, item.MinPayout, item.MinPrice, item.MaxPrice, item.TotAmtGame });
                                                limitsMaster.AgentId = node.AgentID;
                                                limitsMaster.Applied = true;
                                                limitsMaster.MaxWager = item.MaxWager;
                                                limitsMaster.MinWager = item.MinWager;
                                                limitsMaster.MaxPayout = item.MaxPayout;

                                                limitsMaster.MinPrice = item.MinPrice;
                                                limitsMaster.MaxPrice = item.MaxPrice;
                                                limitsMaster.TotAmtGame = item.TotAmtGame;

                                                resp.Add(new LimitsRightsVerificationResp()
                                                {
                                                    AgentId = node.AgentID,
                                                    Code = limitsMaster.Code,
                                                    Message = limitsMaster.Message,
                                                    Applied = true,
                                                    MaxWager = item.MaxWager,
                                                    MinWager = item.MinWager,
                                                    MaxPayout = item.MaxPayout,
                                                    MinPrice = item.MinPrice,
                                                    MaxPrice = item.MaxPrice,
                                                    TotAmtGame = item.TotAmtGame,
                                                });
                                            }


                                        


                                    }
                                }
                                break;
                            case 16:
                                foreach (var node in HierarchyNodes)
                                {


                                    _ = connection.Query<ProfileLimitsResp>(sql, new { node.AgentID, item.IdWagerType, item.SportId, item.LeagueId, item.SportName, item.LeagueName, item.FixtureId, item.MaxWager, item.MinWager, item.MaxPayout, item.MinPayout, item.MinPrice, item.MaxPrice, item.TotAmtGame });
                                    limitsMaster.AgentId = node.AgentID;
                                    limitsMaster.Applied = true;
                                    limitsMaster.MaxWager = item.MaxWager;
                                    limitsMaster.MinWager = item.MinWager;
                                    limitsMaster.MaxPayout = item.MaxPayout;
                                    limitsMaster.MinPrice = item.MinPrice;
                                    limitsMaster.MaxPrice = item.MaxPrice;
                                    limitsMaster.TotAmtGame = item.TotAmtGame;
                                    resp.Add(new LimitsRightsVerificationResp()
                                    {
                                        AgentId = node.AgentID,
                                        Code = limitsMaster.Code,
                                        Message = limitsMaster.Message,
                                        Applied = true,
                                        MaxWager = item.MaxWager,
                                        MinWager = item.MinWager,
                                        MaxPayout = item.MaxPayout,
                                        MinPrice = item.MinPrice,
                                        MaxPrice = item.MaxPrice,
                                        TotAmtGame = item.TotAmtGame,
                                    });
                                }
                                break;
                            case 23:
                                var canupd3 = CanUpdWagerLimits(item.AgentId);

                                if (canupd3)
                                {

                                    _ = connection.Query<ProfileLimitsResp>(sql, values);
                                    limitsMaster.AgentId = item.AgentId;
                                    limitsMaster.Message = "Limits applied, are in of the master agent range.";
                                    limitsMaster.Applied = true;
                                    limitsMaster.MaxWager = item.MaxWager;
                                    limitsMaster.MinWager = item.MinWager;
                                    limitsMaster.MaxPayout = item.MaxPayout;
                                    limitsMaster.MinPrice = item.MinPrice;
                                    limitsMaster.MaxPrice = item.MaxPrice;
                                    limitsMaster.TotAmtGame = item.TotAmtGame;
                                    resp.Add(new LimitsRightsVerificationResp()
                                    {
                                        AgentId = item.AgentId,
                                        Code = 14,
                                        Message = limitsMaster.Message,
                                        Applied = true,
                                        MaxWager = item.MaxWager,
                                        MinWager = item.MinWager,
                                        MaxPayout = item.MaxPayout,
                                        MinPrice = item.MinPrice,
                                        MaxPrice = item.MaxPrice,
                                        TotAmtGame = item.TotAmtGame,
                                    });


                                }
                                else
                                {

                                    if (item.MinWager >= limitsMaster.MinWager && item.MaxWager <= limitsMaster.MaxWager && item.MaxPayout <= limitsMaster.MaxPayout)
                                    {
                                        foreach (var node in HierarchyNodes)
                                        {


                                            if (item.AgentId == node.AgentID)
                                            {
                                                _ = connection.Query<ProfileLimitsResp>(sql, new { node.AgentID, item.IdWagerType, item.SportId, item.LeagueId, item.SportName, item.LeagueName, item.FixtureId, item.MaxWager, item.MinWager, item.MaxPayout, item.MinPayout, item.MinPrice, item.MaxPrice, item.TotAmtGame });
                                                limitsMaster.AgentId = node.AgentID;
                                                resp.Add(new LimitsRightsVerificationResp()
                                                {
                                                    AgentId = node.AgentID,
                                                    Code = limitsMaster.Code,
                                                    Message = limitsMaster.Message,
                                                    Applied = true,
                                                    MaxWager = item.MaxWager,
                                                    MinWager = item.MinWager,
                                                    MaxPayout = item.MaxPayout,
                                                    MinPrice = item.MinPrice,
                                                    MaxPrice = item.MaxPrice,
                                                    TotAmtGame = item.TotAmtGame
                                                });
                                            }
                                            else
                                            {

                                                GetProfileLimitsReq reqMaster = new GetProfileLimitsReq()
                                                {
                                                    AgentId = node.AgentID,
                                                    FixtureId = item.FixtureId,
                                                    IdWagerType = item.IdWagerType,
                                                    LeagueId = item.LeagueId,
                                                    SportId = item.SportId,
                                                    LeagueName = item.LeagueName,
                                                    SportName = item.SportName,
                                                    MaxPayout = item.MaxPayout,
                                                    MinPayout = item.MinPayout,
                                                    MaxWager = item.MaxWager,
                                                    MinWager = item.MinWager,

                                                    MinPrice = item.MinPrice,
                                                    MaxPrice = item.MaxPrice,
                                                    TotAmtGame = item.TotAmtGame,

                                                    ModifiedAt = DateTime.Now
                                                };
                                                var ListReq = new List<GetProfileLimitsReq>
                                        {
                                            reqMaster
                                        };
                                                List<ProfileLimitsResp> ProfileLimitsResp = GetProfileLimits(ListReq);

                                                if (ProfileLimitsResp.Count() > 0)
                                                {



                                                    _ = connection.Query<ProfileLimitsResp>(sql, new { node.AgentID, item.IdWagerType, item.SportId, item.LeagueId, item.SportName, item.LeagueName, item.FixtureId, item.MaxWager, item.MinWager, item.MaxPayout, item.MinPayout, item.MinPrice, item.MaxPrice, item.TotAmtGame });
                                                    limitsMaster.AgentId = node.AgentID;
                                                    limitsMaster.Applied = true;
                                                    limitsMaster.MaxWager = item.MaxWager;
                                                    limitsMaster.MinWager = item.MinWager;
                                                    limitsMaster.MaxPayout = item.MaxPayout;

                                                    limitsMaster.MinPrice = item.MinPrice;
                                                    limitsMaster.MaxPrice = item.MaxPrice;
                                                    limitsMaster.TotAmtGame = item.TotAmtGame;

                                                    resp.Add(new LimitsRightsVerificationResp()
                                                    {
                                                        AgentId = node.AgentID,
                                                        Code =14,
                                                        Message = "Limits applied",
                                                        Applied = true,
                                                        MaxWager = item.MaxWager,
                                                        MinWager = item.MinWager,
                                                        MaxPayout = item.MaxPayout,
                                                        MinPrice = item.MinPrice,
                                                        MaxPrice = item.MaxPrice,
                                                        TotAmtGame = item.TotAmtGame,
                                                    });
                                                }
                                                else
                                                {
                                                    _ = connection.Query<ProfileLimitsResp>(sql, new { node.AgentID, item.IdWagerType, item.SportId, item.LeagueId, item.SportName, item.LeagueName, item.FixtureId, item.MaxWager, item.MinWager, item.MaxPayout, item.MinPayout, item.MinPrice, item.MaxPrice, item.TotAmtGame });
                                                    limitsMaster.AgentId = node.AgentID;
                                                    limitsMaster.Applied = true;
                                                    limitsMaster.MaxWager = item.MaxWager;
                                                    limitsMaster.MinWager = item.MinWager;
                                                    limitsMaster.MaxPayout = item.MaxPayout;

                                                    limitsMaster.MinPrice = item.MinPrice;
                                                    limitsMaster.MaxPrice = item.MaxPrice;
                                                    limitsMaster.TotAmtGame = item.TotAmtGame;

                                                    resp.Add(new LimitsRightsVerificationResp()
                                                    {
                                                        AgentId = node.AgentID,
                                                        Code = 14,
                                                        Message = "Limits applied",
                                                        Applied = true,
                                                        MaxWager = item.MaxWager,
                                                        MinWager = item.MinWager,
                                                        MaxPayout = item.MaxPayout,
                                                        MinPrice = item.MinPrice,
                                                        MaxPrice = item.MaxPrice,
                                                        TotAmtGame = item.TotAmtGame,
                                                    });
                                                }





                                            }
                                        }
                                    }
                                    else
                                    {
                                        resp.Add(new LimitsRightsVerificationResp()
                                        {
                                            AgentId = item.AgentId,
                                            Code =66,
                                            Message = "Limited by Master Agent, out of range.",
                                            Applied = limitsMaster.Applied,
                                            MaxWager = item.MaxWager,
                                            MinWager = item.MinWager,
                                            MaxPayout = item.MaxPayout,

                                            MinPrice = item.MinPrice,
                                            MaxPrice = item.MaxPrice,
                                            TotAmtGame = item.TotAmtGame,
                                        });
                                    }


                                }


                                break;
                            case 51:
                            default:
                                resp.Add(new LimitsRightsVerificationResp()
                                {
                                    AgentId = item.AgentId,
                                    Code = limitsMaster.Code,
                                    Message = limitsMaster.Message,
                                    Applied = limitsMaster.Applied,
                                    MaxWager = item.MaxWager,
                                    MinWager = item.MinWager,
                                    MaxPayout = item.MaxPayout,
                                    MinPrice = item.MinPrice,
                                    MaxPrice = item.MaxPrice,
                                    TotAmtGame = item.TotAmtGame,
                                });
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                resp.Add(new LimitsRightsVerificationResp()
                {
                    AgentId = -1,
                    Code = -1,
                    Message = "Catch a exception: " + ex.Message,
                    Applied = false,
                    MaxWager = 0,
                    MinWager = 0,
                    MaxPayout = 0,
                    MinPrice = 0,
                    MaxPrice = 0,
                    TotAmtGame = 0,
                });
            }
            return resp;
        }

        private bool CanUpdWagerLimits(int prmIdAgent)
        {
            string sql = "AgentRights_CanUpdWagerLimits";
            int result;
            using (var connection = new SqlConnection(dgsConnString))
            {
                using (var command = new SqlCommand(sql, connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;

                    // Agregar el parámetro prmIdAgent
                    command.Parameters.Add(new SqlParameter("@prmIdAgent", prmIdAgent));

                    connection.Open();
                    result = (int)command.ExecuteScalar();
                }
            }

            return result == 1;
        }
        public LimitsRightsVerificationResp CheckLimitsRights(SetProfileLimitsReq req)
        {
            LimitsRightsVerificationResp resp = null;
            AgentTreeResp oAgent = null;
            GetAgentHierarchyResp oAgentDeph = null;
            try
            {
                List<GetAgentHierarchyResp> HierarchyNodes = GetAgentHierarchy(new GetAgentHierarchyReq() { IdAgent = req.AgentId });



                List<AgentTreeResp> HierrarchyList = GetAgentTree(new GetAgentHierarchyReq() { IdAgent = req.AgentId });
                if (HierrarchyList != null)
                {
                    oAgent = HierrarchyList.Where(x => x.IdAgent == req.AgentId).FirstOrDefault();
                }
                if (oAgent != null)
                {
                    oAgentDeph = HierarchyNodes.Where(x => x.AgentID == req.AgentId).FirstOrDefault();
                    var canupd3 = CanUpdWagerLimits(req.AgentId);

                    if (canupd3)
                    {
                        resp = new LimitsRightsVerificationResp()
                        {
                            Code = 14,
                            Message = "Limits applied. ",
                            Applied = true,
                            MaxWager = 0,
                            MinWager = 0,
                            MaxPayout = 0
                        };

                    }

                    else
                    {

                        // cuando es 2 es porque es el Agent Master
                        if (oAgent.AgentLevel == 2)
                        {
                            resp = new LimitsRightsVerificationResp()
                            {
                                Code = 14,
                                Message = "Limits applied, master agent account.",
                                Applied = true,
                                MaxWager = 0,
                                MinWager = 0,
                                MaxPayout = 0
                            };
                        }

                        // cuando es mayor a 2 es porque es un Sub Agent

                        else if (oAgent.AgentLevel > 2)
                        {
                            AgentTreeResp oAgentMaster = HierrarchyList.Where(x => x.AgentLevel == (oAgent.AgentLevel - 1)).FirstOrDefault();
                            if (oAgentMaster != null)
                            {
                                GetProfileLimitsReq reqMaster = new GetProfileLimitsReq()
                                {
                                    AgentId = oAgentMaster.IdAgent,
                                    FixtureId = req.FixtureId,
                                    IdWagerType = req.IdWagerType,
                                    LeagueId = req.LeagueId,
                                    SportId = req.SportId,
                                    SportName = req.SportName,
                                    LeagueName = req.LeagueName,
                                    MaxPayout = req.MaxPayout,
                                    MinPayout = req.MinPayout,
                                    MaxWager = req.MaxWager,
                                    MinWager = req.MinWager,
                                    ModifiedAt = DateTime.Now
                                };
                                var LimitList = new List<GetProfileLimitsReq>() { reqMaster };
                                List<ProfileLimitsResp> ProfileLimitsResp = GetProfileLimits(LimitList);
                                if (ProfileLimitsResp != null && ProfileLimitsResp.Count() > 0)
                                {
                                    resp = new LimitsRightsVerificationResp()
                                    {
                                    Code = 23,
                                        Message = "Limits applied.",
                                        Applied = false,
                                        MaxWager = ProfileLimitsResp[0].MaxWager,
                                        MinWager = ProfileLimitsResp[0].MinWager,
                                        MaxPayout = ProfileLimitsResp[0].MaxPayout
                                    };
                                }
                                else
                                {
                                    resp = new LimitsRightsVerificationResp()
                                    {
                                        Code = 16,
                                        Message = "Limits applied, master agent does not have limits.",
                                        Applied = true,
                                        MaxWager = 0,
                                        MinWager = 0,
                                        MaxPayout = 0
                                    };
                                }
                            }
                        }
                    
                else
                    {
                        resp = new LimitsRightsVerificationResp()
                        {
                            Code = 51,
                            Message = "AgentID does not exists or is Empty.",
                            Applied = false,
                            MaxWager = 0,
                            MinWager = 0,
                            MaxPayout = 0
                        };
                    }

                }
                }
                  
           
            
            }
            catch (Exception ex)
            {
                //throw ex;
            }
            return resp;
        }

        public List<GetAgentHierarchyResp> GetAgentHierarchy(GetAgentHierarchyReq req)
        {
            List<GetAgentHierarchyResp> AgentHierarchyRespList = null;
            //string sql = "select 1 tipo,AgentID,Agent, Depth from fn_SubAgentsOf(@IdAgent) where Depth=0  union  select 2 tipo, AgentID,Agent, Depth from fn_SubAgentsOf(@IdAgent) where Depth<>0  order by 1 asc,2 ASC";
            string sql = "select IdAgent as AgentID, Agent, AgentLevel as Depth from Ft_AgentTree_hierarchy(@IdAgent) order by Hierarchy asc";
            var values = new { req.IdAgent };
            try
            {
                using var connection = new SqlConnection(dgsConnString);
                AgentHierarchyRespList = connection.Query<GetAgentHierarchyResp>(sql, values).ToList();
            }
            catch (Exception ex)
            {
                _ = ex.Message;
            }
            return AgentHierarchyRespList;
        }

        public List<AgentTreeResp> GetAgentTree(GetAgentHierarchyReq req)
        {
            List<AgentTreeResp> AgentTreeRespList = null;
            string sql = "SELECT * FROM Ft_AgentParentsTree(@prmIdagent)";
            var values = new { prmIdagent = req.IdAgent };
            try
            {
                using var connection = new SqlConnection(dgsConnString);
                AgentTreeRespList = connection.Query<AgentTreeResp>(sql, values).ToList();
            }
            catch (Exception ex)
            {
                _ = ex.Message;
            }
            return AgentTreeRespList;
        }

        public ProfileLimitsResp GetProfileLimitsByIdAgent(GetProfileLimitsReq req)
        {
            ProfileLimitsResp ProfileLimitsResp = new ProfileLimitsResp();
            try
            {
                string sql = "exec sp_MGL_GetProfileLimits @AgentId ,@IdWagerType ,@SportId ,@LeagueId ,@FixtureId";
                var values = new { req.AgentId, req.IdWagerType, req.SportId, req.LeagueId, req.FixtureId };
                using var connection = new SqlConnection(moverConnString);
                ProfileLimitsResp = connection.Query<ProfileLimitsResp>(sql, values).FirstOrDefault();

            }
            catch (Exception ex)
            {
                _ = ex.Message;
            }
            return ProfileLimitsResp;
        }

        public List<ProfileLimitsResp> GetProfileLimits(List<GetProfileLimitsReq> req)
        {
            List<ProfileLimitsResp> ProfileLimitsResp = new List<ProfileLimitsResp>();

            try
            {
                if (req.Count() > 0)
                {
                    foreach (var item in req)
                    {
                        string sql = "exec sp_MGL_GetProfileLimits @AgentId ,@IdWagerType ,@SportId ,@LeagueId ,@FixtureId";
                        var values = new { item.AgentId, item.IdWagerType, item.SportId, item.LeagueId, item.FixtureId };
                        using var connection = new SqlConnection(moverConnString);
                        var oResult = connection.Query<ProfileLimitsResp>(sql, values).ToList();
                        ProfileLimitsResp.AddRange(oResult);
                    }
                }

            }
            catch (Exception ex)
            {
                _ = ex.Message;
            }
            return ProfileLimitsResp;
        }

        public GetPlayerLimitsByDGSResp GetPlayerLimitsByDGS(int IdPlayer)
        {
            GetPlayerLimitsByDGSResp playerLimitsByDGSResp = null;
            string sql = "exec WebGetPlayer @IdPlayer";
            var values = new { IdPlayer };
            try
            {
                using var connection = new SqlConnection(dgsConnString);
                playerLimitsByDGSResp = connection.Query<GetPlayerLimitsByDGSResp>(sql, values).FirstOrDefault();
                if (playerLimitsByDGSResp != null)
                {
                    playerLimitsByDGSResp.PL_MaxPayout = GetProfileMaxPayoutDGS(playerLimitsByDGSResp.IdProfile).PL_MaxPayout;
                }
            }
            catch (Exception ex)
            {
                _ = ex.Message;
            }
            return playerLimitsByDGSResp;
        }

        public GetPlayerProfile_GetInfoResp GetProfileMaxPayoutDGS(int IdProfile)
        {
            GetPlayerProfile_GetInfoResp resp = null;
            string sql = "exec PlayerProfile_GetInfo @prmIdProfile";
            var values = new { prmIdProfile = IdProfile };
            try
            {
                using var connection = new SqlConnection(dgsConnString);
                resp = connection.Query<GetPlayerProfile_GetInfoResp>(sql, values).FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return resp;
        }

        public List<DeleteAgentLimitResp> DeleteAgentLimit(List<DeleteAgentLimitReq> req)
        {
            List<DeleteAgentLimitResp> Resp = new List<DeleteAgentLimitResp>();
            string sql = "exec sp_MGL_DeleteAgentLimit @AgentId, @IdWagerType, @SportId, @LeagueId ";
            try
            {
                if (req.Count() > 0)
                {
                    foreach (var oItem in req)
                    {
                        var values = new { oItem.AgentId, oItem.IdWagerType, oItem.SportId, oItem.LeagueId };
                        using var connection = new SqlConnection(moverConnString);
                        connection.Query<DeleteAgentLimitResp>(sql, values);
                        DeleteAgentLimitResp sucess = new DeleteAgentLimitResp()
                        {
                            Code = 14,
                            Message = "Agent Limits deleted succesfully!",
                            ModifiedAt = DateTime.Now
                        };
                        Resp.Add(sucess);
                    }

                }
            }
            catch (Exception ex)
            {
                DeleteAgentLimitResp error = new DeleteAgentLimitResp()
                {
                    Code = 23,
                    Message = $"Error: {ex.Message}",
                    ModifiedAt = DateTime.Now
                };
                Resp.Add(error);
            }
            return Resp;
        }

        public ProfileLimitsByPlayerResp GetProfileLimitsByIdPlayer(ProfileLimitsByPlayerReq req)
        {
            ProfileLimitsByPlayerResp ProfileLimitsResp = null;
            string sql = "exec sp_MGL_GetProfileLimitsByPlayerAgents @PlayerId, @IdWagerType, @SportId, @LeagueId, @FixtureId";

            try
            {
                var values = new { req.PlayerId, req.IdWagerType, req.SportId, req.LeagueId, req.FixtureId };
                using var connection = new SqlConnection(moverConnString);
                ProfileLimitsResp = connection.Query<ProfileLimitsByPlayerResp>(sql, values).FirstOrDefault();

            }
            catch (Exception ex)
            {
                _ = ex.Message;
            }
            return ProfileLimitsResp;
        }

        public List<ProfileLimitsByPlayerResp> GetProfileLimitsByPlayer(List<ProfileLimitsByPlayerReq> req)
        {
            List<ProfileLimitsByPlayerResp> ProfileLimitsResp = new List<ProfileLimitsByPlayerResp>();
            string sql = "exec sp_MGL_GetProfileLimitsByPlayerAgents @PlayerId, @IdWagerType, @SportId, @LeagueId, @FixtureId";

            try
            {
                if (req.Count() > 0)
                {
                    foreach (var oLimit in req)
                    {
                        var values = new { oLimit.PlayerId, oLimit.IdWagerType, oLimit.SportId, oLimit.LeagueId, oLimit.FixtureId, oLimit.MinPrice, oLimit.MaxPrice, oLimit.TotAmtGame };
                        using var connection = new SqlConnection(moverConnString);
                        List<ProfileLimitsByPlayerResp> ProfileLimitsRespAUX = connection.Query<ProfileLimitsByPlayerResp>(sql, values).ToList();
                        if (ProfileLimitsRespAUX.Count() > 0)
                        {
                            ProfileLimitsResp.AddRange(ProfileLimitsRespAUX);
                        }
                        if (ProfileLimitsRespAUX.Count() <= 0)
                        {
                            GetPlayerLimitsByDGSResp DGSDefaultLimits = GetPlayerLimitsByDGS(oLimit.PlayerId);
                            ProfileLimitsByPlayerResp oProfileLimitsDGSResp = new ProfileLimitsByPlayerResp()
                            {
                                PlayerId = oLimit.PlayerId,
                                Id = -1, // Significa que son de DGS
                                FixtureId = oLimit.FixtureId,
                                IdWagerType = oLimit.IdWagerType,
                                LeagueId = oLimit.LeagueId,
                                SportId = oLimit.SportId,
                                SportName = oLimit.SportName,
                                LeagueName = oLimit.LeagueName,
                                MaxWager = DGSDefaultLimits.OnlineMaxWager,
                                MinWager = DGSDefaultLimits.OnlineMinWager,
                                MinPayout = 0,
                                MaxPayout = DGSDefaultLimits.PL_MaxPayout,

                                MinPrice = oLimit.MinPrice,
                                MaxPrice = oLimit.MaxPrice,
                                TotAmtGame = oLimit.TotAmtGame,

                                Type= ProfileLimitsRespAUX[0].Type,


                                ModifiedAt = DateTime.Now
                            };
                            ProfileLimitsResp.Add(oProfileLimitsDGSResp);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                _ = ex.Message;
            }
            return ProfileLimitsResp;
        }

        public LimitsRightsVerificationResp SetProfileLimitsByPlayer(List<ProfileLimitsByPlayerReq> req)
        {
            LimitsRightsVerificationResp Resp = null;
            string sql = "exec sp_MGL_SetProfileLimitsByPlayer  @PlayerId, @IdWagerType ,@SportId ,@LeagueId, @SportName, @LeagueName, @FixtureId, @MaxWager, @MinWager, @MaxPayout, @MinPayout  ,@MinPrice ,@MaxPrice ,@TotAmtGame";

            try
            {
                if (req.Count() > 0)
                {
                    foreach (var oLimit in req)
                    {
                        if (oLimit.SportId != null)
                        {
                            var values = new { oLimit.PlayerId, oLimit.IdWagerType, oLimit.SportId, oLimit.LeagueId, oLimit.SportName, oLimit.LeagueName, oLimit.FixtureId, oLimit.MaxWager, oLimit.MinWager, oLimit.MaxPayout, oLimit.MinPayout, oLimit.MinPrice, oLimit.MaxPrice, oLimit.TotAmtGame };
                            using var connection = new SqlConnection(moverConnString);
                            _ = connection.Query<LimitsRightsVerificationResp>(sql, values);

                            Resp = new LimitsRightsVerificationResp()
                            {
                                Code = 7,
                                Applied = true,
                                Message = "Limits Applied Successfully!",
                                MaxWager = oLimit.MaxWager,
                                MinWager = oLimit.MinWager,
                                MaxPayout = oLimit.MaxPayout,
                                MinPrice = oLimit.MinPrice,
                                MaxPrice = oLimit.MaxPrice,
                                TotAmtGame = oLimit.TotAmtGame,

                       
                               
                               
                               


                            };
                        }
                        else
                        {
                            Resp = new LimitsRightsVerificationResp()
                            {
                                Code = 12,
                                Applied = true,
                                Message = "Error applying limits, SportId cannot be null!",
                                MaxWager = oLimit.MaxWager,
                                MinWager = oLimit.MinWager,
                                MaxPayout = oLimit.MaxPayout,
                                MinPrice = oLimit.MinPrice,
                                MaxPrice = oLimit.MaxPrice,
                                TotAmtGame = oLimit.TotAmtGame,
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _ = ex.Message;
            }
            return Resp;
        }

        public GetVerifiedPasswordByPlayerResp GetVerifiedPasswordByPlayer(GetVerifiedPasswordByPlayerReq req)
        {
            GetVerifiedPasswordByPlayerResp VerifiedPasswordByPlayerResp = null;
            string sql = "exec sp_MGL_GetVerifiedPasswordByPlayer @PlayerId";
            var values = new { req.PlayerId };
            try
            {

                using var connection = new SqlConnection(moverConnString);
                VerifiedPasswordByPlayerResp = connection.Query<GetVerifiedPasswordByPlayerResp>(sql, values).FirstOrDefault();
                if (VerifiedPasswordByPlayerResp == null)
                {
                    VerifiedPasswordByPlayerResp = new GetVerifiedPasswordByPlayerResp()
                    {
                        PlayerId = req.PlayerId,
                        CheckPassword = false,
                        ModifiedAt = DateTime.Now
                    };
                }
            }
            catch (Exception ex)
            {
                _ = ex.Message;
            }
            return VerifiedPasswordByPlayerResp;
        }

        public SetVerifiedPasswordByPlayerResp SetVerifiedPasswordByPlayer(SetVerifiedPasswordByPlayerReq req)
        {
            SetVerifiedPasswordByPlayerResp Resp = new SetVerifiedPasswordByPlayerResp()
            {
                Code = 14,
                Message = "Error completing the action, please try again.",
                ModifiedAt = DateTime.Now
            };
            string sql = "exec sp_MGL_SetVerifiedPasswordByPlayer @PlayerId, @CheckPassword";
            var values = new { req.PlayerId, req.CheckPassword };
            try
            {

                using var connection = new SqlConnection(moverConnString);
                connection.Query<SetVerifiedPasswordByPlayerResp>(sql, values);
                Resp.Code = 7;
                Resp.Message = "Successfull, Verified Password Applied!";

            }
            catch (Exception ex)
            {
                Resp.Code = 3;
                Resp.Message = ex.Message;
            }
            return Resp;
        }

        public SetSportsAndLeaguesHiddenResp SetSportsAndLeaguesHidden(List<SetSportsAndLeaguesHiddenReq> req)
        {
            SetSportsAndLeaguesHiddenResp resp = new SetSportsAndLeaguesHiddenResp()
            {
                Code = 14,
                Message = "Error completing the action, please try again.",
                ModifiedAt = DateTime.Now
            };
            string sql = "exec sp_MGL_SetSportsAndLeaguesHidden @AgentId, @PlayerId, @SportId, @LeagueId, @SportName, @LeagueName, @Enable";
            try
            {
                if (req.Count() > 0)
                {
                    foreach (var oHiddenReq in req)
                    {
                        var values = new { oHiddenReq.AgentId, oHiddenReq.PlayerId, oHiddenReq.SportId, oHiddenReq.LeagueId, oHiddenReq.SportName, oHiddenReq.LeagueName, oHiddenReq.Enable };
                        using var connection = new SqlConnection(moverConnString);
                        connection.Query<SetVerifiedPasswordByPlayerResp>(sql, values);
                        resp.Code = 7;
                        resp.Message = "Successfull, Visibility for Sport/League Changed!";
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

        public List<GetSportsAndLeaguesHiddenResp> GetSportsAndLeaguesHidden(List<GetSportsAndLeaguesHiddenReq> req)
        {
            List<GetSportsAndLeaguesHiddenResp> oListResp = new List<GetSportsAndLeaguesHiddenResp>();
            string sql = "exec sp_MGL_GetSportsAndLeaguesHidden @AgentId, @PlayerId";
            try
            {
                if (req.Count() > 0)
                {
                    foreach (var oItem in req)
                    {
                        var values = new { oItem.AgentId, oItem.PlayerId, oItem.SportId, oItem.LeagueId };
                        using var connection = new SqlConnection(moverConnString);
                        oListResp = connection.Query<GetSportsAndLeaguesHiddenResp>(sql, values).ToList();
                    }

                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return oListResp;
        }

        public GetAccessDeniedListResp GetAccessDeniedById(GetAccessDeniedListReq req)
        {
            GetAccessDeniedListResp oListResp = new GetAccessDeniedListResp();
            string sql = "exec sp_MGL_GetAccessDeniedList_AgentOrPlayer @AgentId, @PlayerId, @AllData";
            try
            {
                var values = new { req.AgentId, req.PlayerId, req.AllData };
                using var connection = new SqlConnection(moverConnString);
                oListResp = connection.Query<GetAccessDeniedListResp>(sql, values).FirstOrDefault();
            }
            catch (Exception ex)
            {
                _ = ex.Message;
            }
            return oListResp;
        }

        public List<GetAccessDeniedListResp> GetAccessDeniedLists(List<GetAccessDeniedListReq> req)
        {
            List<GetAccessDeniedListResp> oListResp = new List<GetAccessDeniedListResp>();
            string sql = "exec sp_MGL_GetAccessDeniedList_AgentOrPlayer @AgentId, @PlayerId, @AllData";
            try
            {
                if (req.Count() > 0)
                {
                    foreach (var oItem in req)
                    {
                        var values = new { 
                            oItem.AgentId, 
                            oItem.PlayerId, 
                            oItem.AllData 
                        };

                        using var connection = new SqlConnection(moverConnString);
                        oListResp.AddRange(connection.Query<GetAccessDeniedListResp>(sql, values).ToList());
                    }

                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return oListResp;
        }

        public List<SetAccessDeniedListResp> SetAccessDeniedLists(List<SetAccessDeniedListReq> req)
        {
            List<SetAccessDeniedListResp> oListResp = new List<SetAccessDeniedListResp>();
            string sql = "exec sp_MGL_SetAccessDeniedList_AgentOrPlayer @Id, @AgentId, @PlayerId, @Enable";
            AgentTreeResp oAgent = null;
            int MasterAgentId = -1;
            try
            {
                if (req.Count() > 0)
                {
                    foreach (var oItem in req)
                    {
                        List<AgentTreeResp> HierrarchyList = GetAgentTree(new GetAgentHierarchyReq() { IdAgent = oItem.AgentId });

                        if (HierrarchyList != null)
                        {
                            oAgent = HierrarchyList.Where(x => x.IdAgent == oItem.AgentId).FirstOrDefault();
                        }
                        if (oAgent != null)
                        {
                            if (oAgent.AgentLevel == 2)
                            {
                                MasterAgentId = oAgent.IdAgent;
                                List<GetAgentHierarchyResp> HierarchyNodes = GetAgentHierarchy(new GetAgentHierarchyReq() { IdAgent = oItem.AgentId });
                                foreach (var node in HierarchyNodes)
                                {
                                    if (node.AgentID == oItem.AgentId)
                                    {
                                        var values = new { oItem.Id, oItem.AgentId, oItem.PlayerId, oItem.Enable };
                                        using var connection = new SqlConnection(moverConnString);
                                        connection.Query<SetAccessDeniedListResp>(sql, values);
                                        SetAccessDeniedListResp oResp = new SetAccessDeniedListResp()
                                        {
                                            Id = oItem.Id,
                                            Code = 14,
                                            Message = "Access changed successfully!",
                                            Enable = oItem.Enable,
                                            AgentId = oItem.AgentId,
                                            PlayerId = oItem.PlayerId
                                        };
                                        oListResp.Add(oResp);
                                    }
                                    else {
                                        var values = new { oItem.Id, node.AgentID, oItem.PlayerId, oItem.Enable };
                                        using var connection = new SqlConnection(moverConnString);
                                        connection.Query<SetAccessDeniedListResp>(sql, values);
                                        SetAccessDeniedListResp oResp = new SetAccessDeniedListResp()
                                        {
                                            Id = oItem.Id,
                                            Code = 14,
                                            Message = "Access changed successfully!",
                                            Enable = oItem.Enable,
                                            AgentId = node.AgentID,
                                            PlayerId = oItem.PlayerId
                                        };
                                        oListResp.Add(oResp);
                                    }
                                }
                            }
                            else
                            {
                                GetAccessDeniedListReq oReqLimit = new GetAccessDeniedListReq()
                                {
                                    AgentId = MasterAgentId,
                                    PlayerId = null,
                                    AllData = false
                                };
                                List<GetAccessDeniedListReq> oList = new List<GetAccessDeniedListReq> {
                                    oReqLimit
                                };
                                var oCheckMasterLimits = GetAccessDeniedLists(oList);
                                if (oCheckMasterLimits.Count() > 0)
                                {
                                    SetAccessDeniedListResp oResp = new SetAccessDeniedListResp()
                                    {
                                        Id = oItem.Id,
                                        Code = 23,
                                        Message = "The changes were not applied, the Master Agent has another configuration applied!",
                                        Enable = oItem.Enable,
                                        AgentId = oItem.AgentId,
                                        PlayerId = oItem.PlayerId
                                    };
                                    oListResp.Add(oResp);
                                }
                                else
                                {
                                    List<GetAgentHierarchyResp> HierarchyNodes = GetAgentHierarchy(new GetAgentHierarchyReq() { IdAgent = oItem.AgentId });
                                    foreach (var node in HierarchyNodes)
                                    {
                                        if (node.AgentID == oItem.AgentId)
                                        {
                                            var valuesHierarchy = new { oItem.Id, oItem.AgentId, oItem.PlayerId, oItem.Enable };
                                            using var connectionHierarchy = new SqlConnection(moverConnString);
                                            connectionHierarchy.Query<SetAccessDeniedListResp>(sql, valuesHierarchy);
                                            SetAccessDeniedListResp oRespHierarchy = new SetAccessDeniedListResp()
                                            {
                                                Id = oItem.Id,
                                                Code = 14,
                                                Message = "Access changed successfully!",
                                                Enable = oItem.Enable,
                                                AgentId = node.AgentID,
                                                PlayerId = oItem.PlayerId
                                            };
                                            oListResp.Add(oRespHierarchy);
                                        }
                                        else
                                        {
                                            var valuesSubHierarchy = new { oItem.Id, node.AgentID, oItem.PlayerId, oItem.Enable };
                                            using var connectionSubHierarchy = new SqlConnection(moverConnString);
                                            connectionSubHierarchy.Query<SetAccessDeniedListResp>(sql, valuesSubHierarchy);
                                            SetAccessDeniedListResp oRespSubHierarchy = new SetAccessDeniedListResp()
                                            {
                                                Id = oItem.Id,
                                                Code = 14,
                                                Message = "Access changed successfully!",
                                                Enable = oItem.Enable,
                                                AgentId = node.AgentID,
                                                PlayerId = oItem.PlayerId
                                            };
                                            oListResp.Add(oRespSubHierarchy);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SetAccessDeniedListResp oResp = new SetAccessDeniedListResp()
                {
                    Code = 13,
                    Message = "Error: Proccess incomplete, please verify the info!",
                    Enable = false,
                    AgentId = -1,
                    PlayerId = -1
                };
                oListResp.Add(oResp);
            }
            return oListResp;
        }

        public List<DeletePlayerLimitResp> DeletePlayerLimit(List<DeletePlayerLimitReq> req)
        {
            List<DeletePlayerLimitResp> Resp = new List<DeletePlayerLimitResp>();
            string sql = "exec sp_MGL_DeletePlayerLimit @Id, @PlayerId";
            try
            {
                if (req.Count() > 0)
                {
                    foreach (var oItem in req)
                    {
                        var values = new { oItem.Id, oItem.PlayerId };
                        using var connection = new SqlConnection(moverConnString);
                        connection.Query<DeletePlayerLimitResp>(sql, values);
                        DeletePlayerLimitResp sucess = new DeletePlayerLimitResp()
                        {
                            Code = 7,
                            Message = "Player Limits deleted succesfully!",
                            ModifiedAt = DateTime.Now
                        };
                        Resp.Add(sucess);
                    }

                }
            }
            catch (Exception ex)
            {
                DeletePlayerLimitResp error = new DeletePlayerLimitResp()
                {
                    Code = 13,
                    Message = $"Error: {ex.Message}",
                    ModifiedAt = DateTime.Now
                };
                Resp.Add(error);
            }
            return Resp;
        }

        public List<DeleteHiddenLeaguesResp> DeleteHiddenLeagues(List<DeleteHiddenLeaguesReq> req)
        {
            List<DeleteHiddenLeaguesResp> Resp = new List<DeleteHiddenLeaguesResp>();
            string sql = "exec sp_MGL_DeleteHiddenLeagues @Id, @AgentId";
            try
            {
                if (req.Count() > 0)
                {
                    foreach (var oItem in req)
                    {
                        var values = new { oItem.Id, oItem.AgentId };
                        using var connection = new SqlConnection(moverConnString);
                        connection.Query<DeletePlayerLimitResp>(sql, values);
                        DeleteHiddenLeaguesResp sucess = new DeleteHiddenLeaguesResp()
                        {
                            Code = 7,
                            Message = "Hidden Leagues deleted succesfully!",
                            ModifiedAt = DateTime.Now
                        };
                        Resp.Add(sucess);
                    }
                }
            }
            catch (Exception ex)
            {
                DeleteHiddenLeaguesResp error = new DeleteHiddenLeaguesResp()
                {
                    Code = 13,
                    Message = $"Error: {ex.Message}",
                    ModifiedAt = DateTime.Now
                };
                Resp.Add(error);
            }
            return Resp;
        }

        public SearchAgentResp GetAgentinfo(SearchAgentReq req)
        {
            SearchAgentResp resp = new SearchAgentResp();
            try
            {
                string sql = "exec VegasLive_SearchAgent @prmS";
                var values = new { prmS = req.Agent };
                using var connection = new SqlConnection(dgsConnString);
                var objDGS = connection.Query<SearchAgentResp>(sql, values).FirstOrDefault();
                if (objDGS != null)
                {
                    resp.AgentInfo = objDGS.AgentInfo;
                }
            }
            catch (Exception ex)
            {

                resp.AgentInfo = ex.Message;
            }
            return resp;
        }


        public SearchPlayertResp GetPlayerinfo(SearchPlayertReq req)
        {
            SearchPlayertResp resp = new SearchPlayertResp();
            try
            {
                string sql = "exec VegasLive_SearchPlayer @prmS";
                var values = new { prmS = req.Player };
                using var connection = new SqlConnection(dgsConnString);
                var objDGS = connection.Query<SearchPlayertResp>(sql, values).FirstOrDefault();
                if (objDGS != null)
                {
                    resp.Player = objDGS.Player;
                    resp.IdPlayer = objDGS.IdPlayer;
                    resp.IdAgent = objDGS.IdAgent;
                }
            }
            catch (Exception ex)
            {

                resp.Player = ex.Message;
            }
            return resp;
        }

        public GetPlayerInfoResp GetPlayerInfo(GetPlayerInfoReq req)
        {
            GetPlayerInfoResp resp = new GetPlayerInfoResp();
            try
            {
                string sql = "exec Player_GetInfoByID @IdPlayer";
                var values = new { IdPlayer = req.PlayerId };
                using var connection = new SqlConnection(dgsConnString);
                var objDGS = connection.Query<GetPlayerInfoResp>(sql, values).FirstOrDefault();
                if (objDGS != null)
                {
                    resp.IdAgent = objDGS.IdAgent;
                }
            }
            catch (Exception ex)
            {

                resp.IdAgent = -1;
            }
            return resp;
        }

        public async Task<LsportsLeagues.Root> GetLsportsLeagues()
        {
            LsportsLeagues.Root resp = new LsportsLeagues.Root();
            try
            {
                string baseUrl = "https://stm-api.lsports.eu/Leagues/Get";
                var jsonData = new
                {
                    PackageId = 1244,
                    UserName = "administracion@corporacionzircon.com",
                    Password = "J83@d784cE",
                    SportIds = Array.Empty<int>(),
                    SubscriptionStatus = "1"
                };
                string jsonString = JsonSerializer.Serialize(jsonData);
                Task<string> jsonTask = Utils.PostData(baseUrl, jsonString);

                // Esperar la finalización de la tarea y obtener la cadena JSON resultante
                string json = await jsonTask;

                // Deserializar la cadena JSON a un objeto
                resp = JsonSerializer.Deserialize<LsportsLeagues.Root>(json);
            }
            catch (Exception ex)
            {
                resp = null;
                //throw ex;
            }
            return resp;
        }

        public async Task<LsportsLeagues.Root> GetLsportsLeaguesByIdSport(GetLsportsLeaguesReq req)
        {
            LsportsLeagues.Root resp = new LsportsLeagues.Root();
            try
            {
                string baseUrl = "https://stm-api.lsports.eu/Leagues/Get";
                var jsonData = new
                {
                    PackageId = 1244,
                    UserName = "administracion@corporacionzircon.com",
                    Password = "J83@d784cE",
                    SportIds = req.IdSports,
                    SubscriptionStatus = "1"
                };
                string jsonString = JsonSerializer.Serialize(jsonData);
                Task<string> jsonTask = Utils.PostData(baseUrl, jsonString);

                // Esperar la finalización de la tarea y obtener la cadena JSON resultante
                string json = await jsonTask;

                // Deserializar la cadena JSON a un objeto
                resp = JsonSerializer.Deserialize<LsportsLeagues.Root>(json);
            }
            catch (Exception ex)
            {
                resp = null;
                //throw ex;
            }
            return resp;
        }

        public async Task<LsportsSports.Root1> GetLsportsSports()
        {
            LsportsSports.Root1 resp = new LsportsSports.Root1();
            try
            {
                string baseUrl = "https://stm-api.lsports.eu/Sports/Get";
                var jsonData = new
                {
                    PackageId = 1244,
                    UserName = "administracion@corporacionzircon.com",
                    Password = "J83@d784cE"
                };
                string jsonString = JsonSerializer.Serialize(jsonData);
                Task<string> jsonTask = Utils.PostData(baseUrl, jsonString);

                // Esperar la finalización de la tarea y obtener la cadena JSON resultante
                string json = await jsonTask;

                // Deserializar la cadena JSON a un objeto
                resp = JsonSerializer.Deserialize<LsportsSports.Root1>(json);
            }
            catch (Exception ex)
            {
                resp = null;
                //throw ex;
            }
            return resp;
        }

        public DgsUserInfo DgsUserLogin(DgsCredentials credentials)
        {
            DgsUserInfo userInfo = new DgsUserInfo();
            try
            {
                using var connection = new SqlConnection(dgsConnString);
                var query = "SELECT IdUser, Name FROM USERS WHERE LoginName = '" + credentials.LoginName + "' and Password = '" + credentials.Password + "' ";
                userInfo = connection.Query<DgsUserInfo>(query).FirstOrDefault();
            }
            catch (Exception ex)
            {

            }
            return userInfo;
        }

        public List<FixtureDto> GetFixturesByDate(FixtureFilter filter)
        {
            List<FixtureDto> fixtures = new List<FixtureDto>();
            try
            {
                using (var connection = new SqlConnection(moverConnString))
                {
                    var parameters = new
                    {
                        eventDate = filter.StartDate.Date,
                        sportId = filter.SportId,
                        leagueId = filter.LeagueId,
                        locationId = filter.LocationId,
                        statusId = filter.StatusId
                    };

                    fixtures = connection.Query<FixtureDto>(@"[dbo].[sp_MGL_GetFixtures]", param: parameters, null, false).ToList();
                }
            }
            catch// (Exception ex)
            {
                //_ = new Misc().WriteErrorLog("MoverDbClass", "GetPendingWagerHeader", ex.Message, ex.StackTrace);
            }

            return fixtures;
        }

        
        public AgentSettings GetAgentSettings(int id)
        {
            var resp = new AgentSettings();
            try
            {
                using (var connection = new SqlConnection(moverConnString))
                {                    
                    resp = connection.QueryFirstOrDefault<AgentSettings>(sql: "sp_MGL_GetAgentSettings", new
                    {
                        idAgent = id
                    }, commandType: CommandType.StoredProcedure);

                }
            }
            catch// (Exception ex)
            {
                //_ = new Misc().WriteErrorLog("MoverDbClass", "GetPendingWagerHeader", ex.Message, ex.StackTrace);
            }

            return resp;
        }

        public AgentSettings GetAgentSettingsForAdmin(int id)
        {
            var resp = new AgentSettings();
            try
            {
                using (var connection = new SqlConnection(moverConnString))
                {
                    resp = connection.QueryFirstOrDefault<AgentSettings>(sql: "sp_MGL_GetAgentSettingsFroAdmin", new
                    {
                        idAgent = id
                    }, commandType: CommandType.StoredProcedure);

                }
            }
            catch// (Exception ex)
            {
                //_ = new Misc().WriteErrorLog("MoverDbClass", "GetPendingWagerHeader", ex.Message, ex.StackTrace);
            }

            return resp;
        }

        public void SaveAgentSettings(AgentSettings settings)
        {            
            try
            {
                using (var connection = new SqlConnection(moverConnString))
                {
                    connection.Execute(sql: "sp_MGL_SetAgentSettings", new
                    {
                        idAgent = settings.IdAgent,
                        secondsDelay = settings.SecondsDelay
                    }, commandType: CommandType.StoredProcedure);
                }
            }
            catch// (Exception ex)
            {
                //_ = new Misc().WriteErrorLog("MoverDbClass", "GetPendingWagerHeader", ex.Message, ex.StackTrace);
            }
        }

    }//end class
}//end namespace
