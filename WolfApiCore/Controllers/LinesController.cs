using Microsoft.AspNetCore.Mvc;
using WolfApiCore.DbTier;
using WolfApiCore.LSportApi;
using WolfApiCore.Models;

namespace WolfApiCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LinesController : ControllerBase
    {

        private readonly IConfiguration _configuration;

        public LinesController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("GetGamesAndLines/{idplayer}")]
        public List<LSport_ScreenSportsDto> GetGamesAndLines(int idPlayer)
        {
            var connString = _configuration.GetValue<string>("SrvSettings:DbConnMover");

            var resultData = new LiveDbClass(connString).GetGamesAndLines(idPlayer);
            return resultData;
        }


        [HttpPost("CreateWager")]
        public LSport_BetSlipObj CreateWager(LSport_BetSlipObj Betslip)
        {
          //  Thread.Sleep(9000); // 5000 milisegundos = 5 segundos
            return new LiveDbWager().ValidateSelectionsForWagers(Betslip);
        }

        [HttpPost("GetHistoryBets")]
        public List<HistoryBetsDTO> GetHistoryBets(HistoryReq req)
        {
            var connString = _configuration.GetValue<string>("SrvSettings:DbConnMover");
            return new LiveDbClass(connString).GetHistoryBets(req);
        }

        [HttpGet("GetPlayerInfo/{idplayer}")]
        public PlayerInfoDto GetPlayerInfo(int idplayer)
        {
            return new LiveDbWager().GetPlayerInfo(idplayer);
        }

        [HttpGet("GetOpenBets/{idplayer}")]
        public List<OpenBetsDTO> GetOpenBets(int idplayer)
        {
            var connString = _configuration.GetValue<string>("SrvSettings:DbConnMover");
            return new LiveDbClass(connString).GetOpenBets(idplayer);
        }

        [HttpGet("GetPlayerDataStreaming/{idplayer}")]
        public PlayerDtoStream GetPlayerDataStreaming(int idplayer)
        {
            return new LiveDbWager().GetPlayerDataStreaming(idplayer);
        }


        [HttpGet("GetPendingLiveWagers")]
        public List<MoverWagerHeaderDto> GetPendingLiveWagers()
        {
            return new LiveDbWager().GetPendingLiveWagers();
        }

        [HttpPost("UpdateWagerDetailResult")]
        public void UpdateWagerDetailResult(GradeDetailWager values)
        {
           // var connString = _configuration.GetValue<string>("SrvSettings:DbConnMover");
            new LiveDbWager().UpdateWagerDetailResult(values.IdLiveWagerDetail, values.IdLiveWager, values.Result, values.IdUser);
        }



        //[HttpGet("test")]
        //public FixtureApiDto test()
        //{
        //    List<int> fix = new List<int>();

        //    fix.Add(11502906);

        //    var obj = new RestApiClass().CallLSportAPI(fix, "1245", "administracion@corporacionzircon.com", "J83@d784cE");

        //    return obj;
        //}

        //[HttpGet("test2/{BetId}")]
        //public string test2(long BetId)
        //{
        //    string result = "";

        //    int MarketId = 52;
        // //   object BetId = 171905384311502906;

        //    List<int> fix = new List<int>();

        //    fix.Add(11481780);

        //    var obj = new RestApiClass().CallLSportAPI(fix, "1245", "administracion@corporacionzircon.com", "J83@d784cE");

        //    if (obj != null)
        //    {
        //        if (obj.Body != null && obj.Body.Count > 0)
        //        {
        //            if (obj.Body[0].Fixture != null)
        //            {
        //                if (obj.Body[0].Fixture.Status == 2) //juego sigue activo
        //                {
        //                    //ahora revisamos la linea
        //                    if (obj.Body[0].Markets != null && obj.Body[0].Markets.Count() > 0)
        //                    {
        //                        var betMarket = obj.Body[0].Markets.Where(x => x.Id == MarketId).FirstOrDefault();

        //                        if (betMarket != null && betMarket.Bets != null && betMarket.Bets.Count() > 0) {

        //                            var betLine = betMarket.Bets.Where(x=> x.Id.ToString() == BetId.ToString()).FirstOrDefault();

        //                            if (betLine != null)
        //                            {
        //                                if (betLine.Status != null && betLine.Status == 1)
        //                                {
        //                                    result = "Encontrada y bien";
        //                                }
        //                                else
        //                                {
        //                                    result = "BetLine Closed";
        //                                }
        //                            }
        //                            else
        //                            {
        //                                result = "Betline does not exist";
        //                            }
        //                        }
        //                        else
        //                        {
        //                            result = "Market closed or It does not exist";
        //                        }
        //                    }
        //                    else
        //                    {
        //                        result = "No Markets available";
        //                    }
        //                }
        //                else
        //                {
        //                    result = "Game Status closed";
        //                }
        //            }
        //            else
        //            {
        //                result = "No Fixture ";
        //            }
        //        }
        //        else
        //        {
        //            result = "No Body";
        //        }
        //    }
        //    else
        //    {
        //        result = "No Data";
        //    }

        //    return result;

        //}//end test


    }
}
