using InfluxdbHelper.Api.Infrastructure;
using InfluxdbHelper.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InfluxdbHelper.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SystemController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IInfluxDBService _influxDbService;

        public SystemController(IConfiguration configuration, IInfluxDBService influxDbService)
        {
            _configuration = configuration;
            _influxDbService = influxDbService;
        }

        /// <summary>InfluxDB 配置状态与连通性（对应旧版首页 Index）。</summary>
        [HttpGet("status")]
        public async Task<IActionResult> Status()
        {
            var url = _configuration["InfluxDBConfig:Url"] ?? string.Empty;
            var token = _configuration["InfluxDBConfig:Token"] ?? string.Empty;
            var org = _configuration["InfluxDBConfig:Org"] ?? string.Empty;
            var bucket = _configuration["InfluxDBConfig:Bucket"] ?? string.Empty;

            var configured = !string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(token)
                             && !string.IsNullOrEmpty(org) && !string.IsNullOrEmpty(bucket);

            var connectionOk = false;
            if (configured)
            {
                connectionOk = await _influxDbService.PingAsync();
            }

            return Ok(ApiResponse.Ok(new
            {
                influxConfigured = configured,
                influxUrl = url,
                influxOrg = org,
                influxBucket = bucket,
                dingTalkEnabled = _configuration.GetValue<bool>("DingTalkConfig:Enabled"),
                connectionOk
            }));
        }
    }
}
