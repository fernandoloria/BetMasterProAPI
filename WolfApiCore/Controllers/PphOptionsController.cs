using Microsoft.AspNetCore.Mvc;
using WolfApiCore.DbTier;
using WolfApiCore.Models;

namespace WolfApiCore.Controllers
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


        [HttpPost("GetOptionsPph")]
        public List<RespPphOptionsModel> Prueba(ReqPphOptionsModel req)
        {
            return new PphOptionsClass().getPphOptions(req);
        }


    }
}
