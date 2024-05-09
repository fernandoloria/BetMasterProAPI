using Newtonsoft.Json;
using System.Net;
using WolfApiCore.Models;

namespace WolfApiCore.Stream
{
    public class StreamService
    {
        public StreamModel GetStream()
        {
            try
            {
                var serviceUrl = $"https://www.livemedia.services/newsource/api.php?token=VGIBTKGHFJDBSNVMCBNVBKEITOGKOSDLFKJKBVCPPF045";
                WebClient wc = new WebClient();
                string result = wc.DownloadString(serviceUrl);
                return JsonConvert.DeserializeObject<StreamModel>(result);
            }
            catch (Exception ex)
            {
                return null;
            }
       }
    }
}
