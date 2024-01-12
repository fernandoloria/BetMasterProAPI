using Microsoft.Extensions.Configuration;

namespace WolfApiCore
{
    public class AppConfig
    {
        private readonly IConfiguration _configuration;

        public AppConfig()
        {
            _configuration = GetConfiguration();


            MinRiskAmount = decimal.Parse(_configuration.GetSection("DefaultBetLimits").GetSection("MinRiskAmount").Value);
            MaxRiskAmount = decimal.Parse(_configuration.GetSection("DefaultBetLimits").GetSection("MaxRiskAmount").Value);
            MaxWinAmount = decimal.Parse(_configuration.GetSection("DefaultBetLimits").GetSection("MaxWinAmount").Value);
        }

        public decimal? MinRiskAmount { get; set; }
        public decimal? MaxRiskAmount { get; set; }
        public decimal? MaxWinAmount { get; set; }

        public IConfigurationRoot GetConfiguration()
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            return builder.Build();
        }


    }
}
