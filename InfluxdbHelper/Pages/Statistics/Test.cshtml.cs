using InfluxdbHelper.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InfluxdbHelper.Pages.Statistics
{
    public class TestModel : PageModel
    {
        private readonly IInfluxDBService _influxDbService;
        private readonly ILogger<TestModel> _logger;

        public TestModel(IInfluxDBService influxDbService, ILogger<TestModel> logger)
        {
            _influxDbService = influxDbService;
            _logger = logger;
        }

        public async Task<ActionResult> OnPostAsync()
        {
            try
            {
                var isConnected = await _influxDbService.PingAsync();
                
                if (isConnected)
                {
                    TempData["TestResult"] = "成功连接到InfluxDB数据库！";
                    TempData["TestSuccess"] = "true";
                }
                else
                {
                    TempData["TestResult"] = "无法连接到InfluxDB数据库，请检查配置。";
                    TempData["TestSuccess"] = "false";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "测试InfluxDB连接时发生错误");
                TempData["TestResult"] = $"测试连接时发生错误：{ex.Message}";
                TempData["TestSuccess"] = "false";
            }

            return Page();
        }
    }
}