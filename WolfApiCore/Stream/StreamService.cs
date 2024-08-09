using Azure.Core;
using Azure;
using Newtonsoft.Json;
using System.Net;
using WolfApiCore.DbTier;
using WolfApiCore.Models;

namespace WolfApiCore.Stream
{
    public static class StreamService
    {
        public static ResponseStreamAccess GetStreamAccess(RequestStreamAccess request)
        {
            var response = StreamDbClass.GetStreamAccessRules(request.IdPlayer);

            if (response.Access == true)
            {
                //response.Url = RusianStreamService.getRussianStream(request);
                response.Url = EzStreamService.getEzStream(request);
                
            }
            return response;
        }
    }
}
