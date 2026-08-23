using InfluxDB.Client;
using InfluxdbHelper.Api.Dtos;
using InfluxdbHelper.Api.Infrastructure;
using InfluxdbHelper.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace InfluxdbHelper.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ConfigController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly IInfluxDBService _influxDbService;

        public ConfigController(IConfiguration configuration, IWebHostEnvironment environment, IInfluxDBService influxDbService)
        {
            _configuration = configuration;
            _environment = environment;
            _influxDbService = influxDbService;
        }

        /// <summary>读取当前配置（Token / Secret 脱敏返回）。</summary>
        [HttpGet]
        public IActionResult Get()
        {
            var resp = new ConfigResponse
            {
                Url = _configuration["InfluxDBConfig:Url"] ?? "http://localhost:8086",
                Token = SecretsMasker.Mask(_configuration["InfluxDBConfig:Token"]),
                Org = _configuration["InfluxDBConfig:Org"] ?? string.Empty,
                Bucket = _configuration["InfluxDBConfig:Bucket"] ?? string.Empty,
                DingTalkWebhookUrl = _configuration["DingTalkConfig:WebhookUrl"] ?? string.Empty,
                DingTalkSecret = SecretsMasker.Mask(_configuration["DingTalkConfig:Secret"]),
                DingTalkEnabled = _configuration.GetValue<bool>("DingTalkConfig:Enabled"),
                DingTalkSendHour = _configuration.GetValue<int>("DingTalkConfig:SendHour", 9),
                DingTalkSendMinute = _configuration.GetValue<int>("DingTalkConfig:SendMinute", 0),
                DingTalkMessageTemplate = _configuration["DingTalkConfig:MessageTemplate"] ?? DefaultTemplate
            };
            return Ok(ApiResponse.Ok(resp));
        }

        /// <summary>保存配置并热重载 InfluxDB 客户端（对应旧版 Config 页 OnPost）。</summary>
        [HttpPut]
        public async Task<IActionResult> Save([FromBody] ConfigUpdateRequest request)
        {
            // 敏感字段：留空或传回脱敏占位值 → 沿用现有配置
            var currentToken = _configuration["InfluxDBConfig:Token"] ?? string.Empty;
            var currentSecret = _configuration["DingTalkConfig:Secret"] ?? string.Empty;
            var token = SecretsMasker.IsMaskedOrEmpty(request.Token) ? currentToken : request.Token!;
            var secret = SecretsMasker.IsMaskedOrEmpty(request.DingTalkSecret) ? currentSecret : request.DingTalkSecret!;

            string? persistError = null;
            if (request.Persist)
            {
                persistError = await TryPersistToFile(request, token, secret);
                if (persistError != null)
                {
                    return Ok(ApiResponse.Fail(2001, $"配置未保存：{persistError}（文件系统可能只读，可尝试关闭\"写入配置文件\"选项）"));
                }

                // appsettings.json 变更触发 reloadOnChange 后再重建客户端，等待配置重载完成
                await Task.Delay(300);
            }

            var reinitOk = await _influxDbService.ReinitializeClientAsync();
            var connectionOk = reinitOk && await _influxDbService.PingAsync();

            return Ok(ApiResponse.Ok(new ConfigSaveResult
            {
                Saved = request.Persist,
                ConnectionOk = connectionOk,
                Error = connectionOk ? null : "数据库连接测试失败，请检查配置信息是否正确"
            }, connectionOk ? "配置已保存，连接测试成功" : "配置已保存，但连接测试失败"));
        }

        /// <summary>仅测试连接，不保存任何变更。</summary>
        [HttpPost("test")]
        public async Task<IActionResult> Test([FromBody] ConnectionTestRequest request)
        {
            var url = request.Url.TrimEnd('/');
            if (!Uri.TryCreate(url, UriKind.Absolute, out _))
            {
                return Ok(ApiResponse.Fail(2002, "URL 格式不正确"));
            }

            var token = SecretsMasker.IsMaskedOrEmpty(request.Token)
                ? _configuration["InfluxDBConfig:Token"]
                : request.Token;

            try
            {
                using var client = string.IsNullOrEmpty(token)
                    ? new InfluxDBClient(url)
                    : new InfluxDBClient(url, token);

                var ok = await client.PingAsync();
                return Ok(ApiResponse.Ok(new { connectionOk = ok },
                    ok ? "连接成功" : "连接失败：InfluxDB 无响应"));
            }
            catch (Exception ex)
            {
                return Ok(ApiResponse.Fail(2003, $"连接失败: {ex.Message}"));
            }
        }

        /// <summary>把配置写入 appsettings.json（与旧版 ConfigModel.SaveConfigToFile 逻辑一致）。</summary>
        private async Task<string?> TryPersistToFile(ConfigUpdateRequest request, string token, string secret)
        {
            try
            {
                var configPath = Path.Combine(_environment.ContentRootPath, "appsettings.json");
                var configJson = JObject.Parse(await System.IO.File.ReadAllTextAsync(configPath));

                configJson["InfluxDBConfig"]!["Url"] = request.Url;
                configJson["InfluxDBConfig"]!["Token"] = token;
                configJson["InfluxDBConfig"]!["Org"] = request.Org;
                configJson["InfluxDBConfig"]!["Bucket"] = request.Bucket;

                configJson["DingTalkConfig"]!["WebhookUrl"] = request.DingTalkWebhookUrl;
                configJson["DingTalkConfig"]!["Secret"] = secret;
                configJson["DingTalkConfig"]!["Enabled"] = request.DingTalkEnabled;
                configJson["DingTalkConfig"]!["SendHour"] = request.DingTalkSendHour;
                configJson["DingTalkConfig"]!["SendMinute"] = request.DingTalkSendMinute;
                configJson["DingTalkConfig"]!["MessageTemplate"] = string.IsNullOrEmpty(request.DingTalkMessageTemplate)
                    ? DefaultTemplate
                    : request.DingTalkMessageTemplate;

                await System.IO.File.WriteAllTextAsync(configPath, configJson.ToString());
                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private const string DefaultTemplate = @"## InfluxDB 数据统计报告 ({{date}})

### 数据概览
- **总数据条数**: {{total_count}}
- **统计时间**: {{start_time}} 至 {{end_time}}

### 变量数据分布
{{variable_stats}}";
    }
}
