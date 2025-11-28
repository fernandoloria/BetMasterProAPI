using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using BetMasterApiCore.DbTier;
using BetMasterApiCore.LSportApi;
using BetMasterApiCore.Models;



namespace BetMasterApiCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ScoreController : ControllerBase
    {

        [HttpPost("InsertScore")]
        public RespScore InsertScores(List<ReqScore> req)
        {
            return new ScoreDbClass().InsertScores(req);
        }


        [HttpPost("GetScores")]
        public RespSportData GetScores(ReqGetScores req)
        {
            return new ScoreDbClass().GetScores(req);
        }

        [HttpGet("GetAllSport")]
        public List<RespAllSport> GetAllSport()
        {
            return new ScoreDbClass().GetAllSport();
        }


        [HttpPost("GetFilteredLeagues")]
        public List<RespFilteredLeague> GetFilteredLeagues(ReqFilteredLeague req)
        {
            return new ScoreDbClass().GetFilteredLeagues(req);
        }





    }
}
