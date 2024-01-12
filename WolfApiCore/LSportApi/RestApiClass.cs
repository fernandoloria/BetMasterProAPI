using Newtonsoft.Json;
using System.Net;
using System.Text;
using WolfApiCore.Models;

namespace WolfApiCore.LSportApi
{
    public class RestApiClass
    {

       // static readonly HttpClient client = new HttpClient();

        public class CallRequest
        {
            public int PackageId { get; set; }
            public string UserName { get; set; }
            public string Password { get; set; }
            public int FromDate { get; set; }
            public List<int> Fixtures { get; set; }
        }

        public FixtureApiDto CallLSportAPI(List<int> FixtureList, string PackageId,  string userName, string password)
        {
            FixtureApiDto resultValue = new FixtureApiDto();
            string ResponseString;
            HttpWebResponse response;
            try
            {


                string baseUrl = "https://stm-snapshot.lsports.eu/InPlay/GetEvents";


                CallRequest requestObj = new CallRequest
                {
                    UserName = userName, //"marvin.gl@gmail.com",
                    PackageId = Convert.ToInt32(PackageId),
                    Password = password, //"J83@d784cE",
                    Fixtures = FixtureList
                };



                var request = (HttpWebRequest)WebRequest.Create(baseUrl);
                request.Accept = "application/json";
                request.Method = "POST";
                //  request.Headers["Authorization"] = "Bearer fP3yzoJNPrJknHmeyzLuKxqWzaQ8deBSeskrsFec2Gw";

                var myContent = JsonConvert.SerializeObject(requestObj);

                var data = Encoding.ASCII.GetBytes(myContent);

                request.ContentType = "application/json";
                request.ContentLength = data.Length;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }
                using (response = (HttpWebResponse)request.GetResponse())
                {
                    ResponseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                    resultValue = JsonConvert.DeserializeObject<FixtureApiDto>(ResponseString);
                }
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    response = (HttpWebResponse)ex.Response;
                    ResponseString = "Some error occured: " + response.StatusCode.ToString();
                }
                else
                {
                    ResponseString = "Some error occured: " + ex.Status.ToString();
                }
            }

            return resultValue;

        }//end method



    }//end class
}//end namespace
