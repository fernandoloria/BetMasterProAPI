using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using BetMasterApiCore.DbTier;
using BetMasterApiCore.Models;
using BetMasterApiCore.Stream;
using BetMasterApiCore.Utilities;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BetMasterApiCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LinesController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly Base64Service _base64Service;

        public LinesController(IConfiguration configuration, Base64Service base64Service)
        {
            _configuration = configuration;
            _base64Service = base64Service;
        }


        [HttpPost("Auth/Exchange")]
        public IActionResult ExchangeToken([FromBody] ExchangeRequest request, [FromServices] JwtService jwtService)
        {
            if (request == null || string.IsNullOrEmpty(request.Piden) || request.SiteId <= 0)
                return BadRequest(new { code = "INVALID_REQUEST", message = "Missing piden or siteId." });

            try
            {
                var dataAccess = new LiveDbClass();
                SiteInfo site = dataAccess.GetSiteInfo(request.SiteId);

                if (site == null)
                    return Unauthorized(new { code = "INVALID_SITE", message = "Site not found." });

                if (!site.IsActive)
                    return StatusCode(403, new { code = "SITE_INACTIVE", message = "This site is suspended." });

                string json = CryptoHelper.DecryptAes(request.Piden, site.SecretKey);
                if (string.IsNullOrEmpty(json))
                    return Unauthorized(new { code = "DECRYPT_ERROR", message = "Invalid or expired token." });

                dynamic data = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
                int idPlayer = Convert.ToInt32(data.idPlayer);
                int idCall = Convert.ToInt32(data.idCall);

                string jwt = jwtService.GenerateJwt(idPlayer, idCall, site.SiteId);

                site.SecretKey = null;
                var response = new ExchangeResponse
                {
                    Jwt = jwt,
                    IdPlayer = idPlayer,
                    IdCall = idCall
                    //,Site = site
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { code = "SERVER_ERROR", message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("GetLastBetHours")]
        public RespPlayerLastBet GetLastBetHours(ReqPlayerLastBet idplayer)
        {
            return new LiveDbWager().GetLastBetHours(idplayer.idPlayer);
        }

        [Authorize]
        [HttpGet("GetGamesAndLines")]
        public List<LSport_ScreenSportsDto> GetGamesAndLines()
        {

            var identity = (ClaimsIdentity)User.Identity;

            var idPlayerClaim = identity.FindFirst("idPlayer")?.Value;
            var idCallClaim = identity.FindFirst("idCall")?.Value;
            var siteIdClaim = identity.FindFirst("siteId")?.Value;

            if (idPlayerClaim == null || idCallClaim == null || siteIdClaim == null)
                throw new UnauthorizedAccessException("Missing required claims in token.");

            int idPlayer = Convert.ToInt32(idPlayerClaim);
            int idCall = Convert.ToInt32(idCallClaim);
            int siteId = Convert.ToInt32(siteIdClaim);

            var connString = _configuration.GetValue<string>("SrvSettings:DbConnMover");
            //var resultData = new LiveDbClass().GetGamesAndLines(idPlayer);
            var resultData = new LiveDbClass().GetGamesAndLinesV2(idPlayer);
            
            return resultData;
        }

        [Authorize]
        [HttpGet("GetGamesAndLinesV2")]
        public List<LSport_ScreenSportsDto> GetGamesAndLinesV2()
        {
            var identity = (ClaimsIdentity)User.Identity;

            var idPlayerClaim = identity.FindFirst("idPlayer")?.Value;
            var idCallClaim = identity.FindFirst("idCall")?.Value;
            var siteIdClaim = identity.FindFirst("siteId")?.Value;

            if (idPlayerClaim == null || idCallClaim == null || siteIdClaim == null)
                throw new UnauthorizedAccessException("Missing required claims in token.");

            int idPlayer = Convert.ToInt32(idPlayerClaim);
            int idCall = Convert.ToInt32(idCallClaim);
            int siteId = Convert.ToInt32(siteIdClaim);

            var connString = _configuration.GetValue<string>("SrvSettings:DbConnMover");
            var resultData = new LiveDbClass().GetGamesAndLinesV2(idPlayer);
            return resultData;
        }

        [Authorize]
        [HttpPost("CreateWager")]
        public LSport_BetSlipObj CreateWager(LSport_BetSlipObj Betslip)
        {
          //  Thread.Sleep(9000); // 5000 milisegundos = 5 segundos
            return new LiveDbWager().ValidateSelectionsForWagers(Betslip);
        }

        [Authorize]
        [HttpPost("GetHistoryBets")]
        public List<HistoryBetsDTO> GetHistoryBets(HistoryReq req)
        {
            var connString = _configuration.GetValue<string>("SrvSettings:DbConnMover");
            return new LiveDbClass().GetHistoryBets(req);
        }

        [Authorize]
        [HttpGet("GetPlayerInfo")]
        public PlayerInfoDto GetPlayerInfo()
        {
            var identity = (ClaimsIdentity)User.Identity;

            var idPlayerClaim = identity.FindFirst("idPlayer")?.Value;
            var idCallClaim = identity.FindFirst("idCall")?.Value;
            var siteIdClaim = identity.FindFirst("siteId")?.Value;

            if (idPlayerClaim == null || idCallClaim == null || siteIdClaim == null)
                throw new UnauthorizedAccessException("Missing required claims in token.");

            int idPlayer = Convert.ToInt32(idPlayerClaim);
            int idCall = Convert.ToInt32(idCallClaim);
            int siteId = Convert.ToInt32(siteIdClaim);

            return new LiveDbWager().GetPlayerInfo(idPlayer, idCall);
        }

        [Authorize]
        [HttpGet("GetOpenBets")]
        public List<OpenBetsDTO> GetOpenBets()
        {
            var identity = (ClaimsIdentity)User.Identity;

            var idPlayerClaim = identity.FindFirst("idPlayer")?.Value;
            var idCallClaim = identity.FindFirst("idCall")?.Value;
            var siteIdClaim = identity.FindFirst("siteId")?.Value;

            if (idPlayerClaim == null || idCallClaim == null || siteIdClaim == null)
                throw new UnauthorizedAccessException("Missing required claims in token.");

            int idPlayer = Convert.ToInt32(idPlayerClaim);
            int idCall = Convert.ToInt32(idCallClaim);
            int siteId = Convert.ToInt32(siteIdClaim);

            var connString = _configuration.GetValue<string>("SrvSettings:DbConnMover");
            return new LiveDbClass().GetOpenBets(idPlayer);
        }

        [Authorize]
        [HttpGet("GetPlayerDataStreaming/{idplayer}")]
        public PlayerDtoStream GetPlayerDataStreaming(int idplayer)
        {
            return new LiveDbWager().GetPlayerDataStreaming(idplayer);
        }

        [Authorize]
        [HttpGet("GetPendingLiveWagers")]
        public List<MoverWagerHeaderDto> GetPendingLiveWagers()
        {
            return new LiveDbWager().GetPendingLiveWagers();
        }


        [Authorize]
        [HttpPost("UpdateWagerDetailResult")]
        public async Task<ActionResult> UpdateWagerDetailResult(GradeDetailWager values)
        {
           // var connString = _configuration.GetValue<string>("SrvSettings:DbConnMover");
            new LiveDbWager().UpdateWagerDetailResult(values.IdLiveWagerDetail, values.IdLiveWager, values.Result, values.IdUser);

            return NoContent();
        }

        [Authorize]
        [HttpGet("GetPlayerInfoByIdCall")]
        public IActionResult GetPlayerInfoByIdCall()
        {
            var identity = (ClaimsIdentity)User.Identity;

            var idPlayerClaim = identity.FindFirst("idPlayer")?.Value;
            var idCallClaim = identity.FindFirst("idCall")?.Value;
            var siteIdClaim = identity.FindFirst("siteId")?.Value;

            if (idPlayerClaim == null || idCallClaim == null || siteIdClaim == null)
                throw new UnauthorizedAccessException("Missing required claims in token.");

            int idPlayer = Convert.ToInt32(idPlayerClaim);
            int idCall = Convert.ToInt32(idCallClaim);
            int siteId = Convert.ToInt32(siteIdClaim);


            PlayerInfoDto? player = new LiveDbWager().GetPlayerInfo(idPlayer, idCall);

            if ( player is null )
            {
               return Unauthorized(new {
                    code = "SESSION_INVALID",
                    message = "Renew Session"
               }); 
            }

            return Ok(player);
        }

        


    }
}
