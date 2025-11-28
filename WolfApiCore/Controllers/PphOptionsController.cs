using Microsoft.AspNetCore.Mvc;
using BetMasterApiCore.DbTier;
using BetMasterApiCore.Models;

namespace BetMasterApiCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PphOptionsController
    {

        [HttpPost("GetOptionsPph")]
        public List<RespPphOptionsModel> getPphOptions(ReqPphOptionsModel req)
        {
            return new PphOptionsClass().getPphOptions(req);
        }


        [HttpPost("GetOptionsPph2")]
        public List<RespPphOptionsModel> Prueba(ReqPphOptionsModel req)
        {
            return new PphOptionsClass().getPphOptions(req);
        }


    }
}
