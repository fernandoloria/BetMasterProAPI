using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using WolfApiCore.DbTier;
using WolfApiCore.Hubs;
using WolfApiCore.Utilities;

namespace WolfApiCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SignalRController : ControllerBase
    {
        private readonly IHubContext<Messages> _hubContext;
        private readonly IConfiguration _configuration;

       // private readonly IHubContext<MessagesPb> _hubContextpb;

        public SignalRController(IHubContext<Messages> hubContext,/* IHubContext<MessagesPb> hubContextpb,*/ IConfiguration configuration)
        {
            _hubContext = hubContext;
         //   _hubContextpb = hubContextpb;
            _configuration = configuration;
        }


        [HttpGet("GetChangedOrNewScores/{FixtureId}")]
        public async Task<IActionResult> GetChangedOrNewScores(int FixtureId)
        {            
            var ipAddress = HttpContext.GetRemoteIPAddress().ToString();
            var connString = _configuration.GetValue<string>("SrvSettings:DbConnMover");
            var dataAccess = new LiveDbClass(connString);

            //dataAccess.WriteSignalRUpdaterIP(ipAddress);            

            var gamesAndLines = dataAccess.GetSignalFixtures();
            await _hubContext.Clients.All.SendAsync("SendAllGamesAndLines", gamesAndLines);
            return Ok(new { resp = "all is ok" });
        }

        //[HttpGet("GetChangedOrNewScoresPb/{FixtureId}")]
        //public async Task<IActionResult> GetChangedOrNewScorespb(int FixtureId)
        //{
        //    //  var gamesAndLines = new LiveDbClass().GetGamesAndLines(hour);
        //    var connString = _configuration.GetValue<string>("SrvSettings:DbConnMover");

        //    var gamesAndLines = new PropBuildDbClass(connString).GetSignalFixtures();

        //    await _hubContextpb.Clients.All.SendAsync("SendAllGamesAndLinesPb", gamesAndLines);

        //    return Ok(new { resp = "all is ok pb" });
        //}

    }
}
