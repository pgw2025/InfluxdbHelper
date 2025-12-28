using InfluxdbHelper.Models;
using InfluxdbHelper.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json.Linq;

namespace InfluxdbHelper.Pages
{
    public class ConfigModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly IInfluxDBService _influxDbService;

        public ConfigModel(IConfiguration configuration, IWebHostEnvironment environment, IInfluxDBService influxDbService)
        {
            _configuration = configuration;
            _environment = environment;
            _influxDbService = influxDbService;
        }

        [BindProperty]
        public InfluxDBConfigFormModel Config { get; set; } = new InfluxDBConfigFormModel();

        [BindProperty]
        public bool SaveToConfigFile { get; set; } = false;

        public void OnGet()
        {
            // 从配置中加载当前值
            Config = new InfluxDBConfigFormModel
            {
                Url = _configuration.GetSection("InfluxDBConfig:Url").Value ?? "http://localhost:8086",
                Token = _configuration.GetSection("InfluxDBConfig:Token").Value ?? string.Empty,
                Org = _configuration.GetSection("InfluxDBConfig:Org").Value ?? string.Empty,
                Bucket = _configuration.GetSection("InfluxDBConfig:Bucket").Value ?? string.Empty,
                DingTalkWebhookUrl = _configuration.GetSection("DingTalkConfig:WebhookUrl").Value ?? string.Empty,
                DingTalkSecret = _configuration.GetSection("DingTalkConfig:Secret").Value ?? string.Empty,
                DingTalkEnabled = _configuration.GetValue<bool>("DingTalkConfig:Enabled", true),
                DingTalkSendHour = _configuration.GetValue<int>("DingTalkConfig:SendHour", 9),
                DingTalkSendMinute = _configuration.GetValue<int>("DingTalkConfig:SendMinute", 0),
                DingTalkMessageTemplate = _configuration.GetSection("DingTalkConfig:MessageTemplate").Value ?? @"## InfluxDB 数据统计报告 ({{date}})

### 数据概览
- **总数据条数**: {{total_count}}
- **统计时间**: {{start_time}} 至 {{end_time}}

### 变量数据分布
{{variable_stats}}"
            };
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // 如果用户选择保存到配置文件
                if (SaveToConfigFile)
                {
                    await SaveConfigToFile();
                }

                // 重新初始化InfluxDB服务
                var success = await _influxDbService.ReinitializeClientAsync();

                if (success)
                {
                    TempData["SuccessMessage"] = "配置信息已更新，数据库连接测试成功！";
                }
                else
                {
                    TempData["ErrorMessage"] = "配置信息已更新，但数据库连接测试失败。请检查配置信息是否正确。";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "配置更新失败: " + ex.Message;
            }

            return Page();
        }

        private async Task SaveConfigToFile()
        {
            var configPath = Path.Combine(_environment.ContentRootPath, "appsettings.json");
            var configText = await System.IO.File.ReadAllTextAsync(configPath);
            var configJson = Newtonsoft.Json.Linq.JObject.Parse(configText);

            // 更新InfluxDB配置
            configJson["InfluxDBConfig"]["Url"] = Config.Url;
            configJson["InfluxDBConfig"]["Token"] = Config.Token;
            configJson["InfluxDBConfig"]["Org"] = Config.Org;
            configJson["InfluxDBConfig"]["Bucket"] = Config.Bucket;

            // 更新钉钉配置
            configJson["DingTalkConfig"]["WebhookUrl"] = Config.DingTalkWebhookUrl;
            configJson["DingTalkConfig"]["Secret"] = Config.DingTalkSecret;
            configJson["DingTalkConfig"]["Enabled"] = Config.DingTalkEnabled;
            configJson["DingTalkConfig"]["SendHour"] = Config.DingTalkSendHour;
            configJson["DingTalkConfig"]["SendMinute"] = Config.DingTalkSendMinute;
            configJson["DingTalkConfig"]["MessageTemplate"] = Config.DingTalkMessageTemplate;

            await System.IO.File.WriteAllTextAsync(configPath, configJson.ToString());
        }
    }
}