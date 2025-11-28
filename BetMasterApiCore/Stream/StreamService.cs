using Azure.Core;
using Azure;
using Newtonsoft.Json;
using System.Net;
using BetMasterApiCore.DbTier;
using BetMasterApiCore.Models;

namespace BetMasterApiCore.Stream
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
