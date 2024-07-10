using Newtonsoft.Json;
using System.Net;
using WolfApiCore.DbTier;
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

        public void PushNotification(BroadcastNotification notification) 
        {
            switch (notification.type) {
                case 1:
                case 2: StreamDbClass.PushNotification(notification); //insert, update
                    break;

                case 3: StreamDbClass.DeleteNotification(notification);//delete
                    break;

                default: throw new Exception("Unknown notification type");
            }            
        }
    }
}
