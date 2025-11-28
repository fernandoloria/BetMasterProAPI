using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BetMasterApiCore.Utilities
{
    public static class Utils
    {
        public static async Task<string> PostData(string baseUrl, string jsonData)
        {
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
            try
            {
                var response = await new HttpClient().PostAsync(baseUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    string respuesta = await response.Content.ReadAsStringAsync();
                    return respuesta;
                }
                else
                {
                    return null;
                }
            }
            catch (HttpRequestException e)
            {
                return null;
            }
        }

        public static async Task<string> GetData(string baseUrl)
        {
            try
            {
                var response = await new HttpClient().GetAsync(baseUrl);

                if (response.IsSuccessStatusCode)
                {
                    string respuesta = await response.Content.ReadAsStringAsync();
                    return respuesta;
                }
                else
                {
                    return null;
                }
            }
            catch (HttpRequestException e)
            {
                return null;
            }
        }

    }
}
