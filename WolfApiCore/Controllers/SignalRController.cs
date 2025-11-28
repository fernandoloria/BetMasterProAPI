using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using BetMasterApiCore.DbTier;
using BetMasterApiCore.Hubs;
using BetMasterApiCore.Utilities;
using Microsoft.AspNetCore.Authorization;

namespace BetMasterApiCore.Controllers
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


        [Authorize]
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

       

    }
}
