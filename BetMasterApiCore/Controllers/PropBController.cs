using Microsoft.AspNetCore.Mvc;
using BetMasterApiCore.DbTier;
using BetMasterApiCore.Models;

namespace BetMasterApiCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PropBController : ControllerBase
    {

        private readonly IConfiguration _configuration;

        public PropBController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("GetGamesAndLines/{idplayer}")]
        public List<LSport_ScreenSportsDto> GetGamesAndLines(int idPlayer)
        {
            var connString = _configuration.GetValue<string>("SrvSettings:DbConnMover");

            var resultData = new PropBuildDbClass(connString).GetGamesAndLines(idPlayer);
            return resultData;
        }

        [HttpPost("CreateWager")]
        public LSport_BetSlipObj CreateWager(LSport_BetSlipObj Betslip)
        {
           // Thread.Sleep(7000); // 5000 milisegundos = 5 segundos
            return new PbDbWagerClass().ValidateSelectionsForWagers(Betslip);
        }

    }
}
