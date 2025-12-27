using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InfluxdbHelper.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(IConfiguration configuration, ILogger<IndexModel> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public string InfluxDBStatus { get; set; } = "未配置";
        public string InfluxDBUrl { get; set; } = "";

        public void OnGet()
        {
            var influxUrl = _configuration.GetSection("InfluxDBConfig:Url").Value;
            var influxToken = _configuration.GetSection("InfluxDBConfig:Token").Value;
            var influxOrg = _configuration.GetSection("InfluxDBConfig:Org").Value;
            var influxBucket = _configuration.GetSection("InfluxDBConfig:Bucket").Value;

            if (!string.IsNullOrEmpty(influxUrl) &&
                !string.IsNullOrEmpty(influxToken) &&
                !string.IsNullOrEmpty(influxOrg) &&
                !string.IsNullOrEmpty(influxBucket))
            {
                InfluxDBStatus = "已配置";
                InfluxDBUrl = influxUrl;
            }
            else
            {
                InfluxDBStatus = "未配置完整";
            }
        }
    }
}
