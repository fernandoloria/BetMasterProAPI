using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WolfApiCore.DbTier;
using WolfApiCore.Models;
using WolfApiCore.Stream;

namespace WolfApiCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StreamingController : ControllerBase
    {
        [HttpPost("PushNotification")]
        public IActionResult PushNotification(BroadcastNotification notification)
        {
            try {
                new StreamService().PushNotification(notification);
                return Ok("Notification Push Succesfull");
            }
            catch (Exception ex) {
                return BadRequest(ex.Message);
            }   
        }

        [HttpGet("GetSignature")]
        public string GetSignature()
        {
            return new SignatureGenerator().GenerateSignature();
        }

        [HttpPost("GetStreamAccess")]
        public GetStreamAccessDTO GetStreamAccess(RequestStreamAccess request)
        {
            return StreamDbClass.GetStreamAccess(request);
        }

    }
}
