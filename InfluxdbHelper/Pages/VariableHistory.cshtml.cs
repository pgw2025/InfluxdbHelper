using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using InfluxdbHelper.DTOs;
using InfluxdbHelper.Services;
using InfluxdbHelper.Models;
using NodaTime;

namespace InfluxdbHelper.Pages
{
    public class VariableHistoryModel : PageModel
    {
        private readonly IInfluxDBService _influxDbService;
        private readonly IConfiguration _configuration;

        public VariableHistoryModel(IInfluxDBService influxDbService, IConfiguration configuration)
        {
            _influxDbService = influxDbService;
            _configuration = configuration;
        }

        [BindProperty]
        public string VariableName { get; set; } = string.Empty;

        [BindProperty]
        public DateTime? StartTime { get; set; }

        [BindProperty]
        public DateTime? EndTime { get; set; }

        public List<VariableValueDto> QueryResults { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            // 设置默认时间范围为最近24小时
            if (!EndTime.HasValue)
            {
                EndTime = DateTime.Now;
            }
            if (!StartTime.HasValue)
            {
                StartTime = EndTime.Value.AddHours(-24);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(VariableName))
            {
                TempData["ErrorMessage"] = "请提供变量名称";
                return Page();
            }

            if (!StartTime.HasValue || !EndTime.HasValue)
            {
                TempData["ErrorMessage"] = "请提供完整的时间范围";
                return Page();
            }

            if (StartTime.Value > EndTime.Value)
            {
                TempData["ErrorMessage"] = "开始时间不能晚于结束时间";
                return Page();
            }

            try
            {
                // 获取配置中的bucket名称
                var config = _configuration.GetSection("InfluxDBConfig").Get<InfluxDBConfig>();
                var bucket = config.Bucket;

                // 构建查询语句 - 使用更灵活的过滤方式
                // 在InfluxDB中，变量名通常作为field，但有时也可能作为tag
                var query = $"from(bucket: \"{bucket}\") " +
                           $"|> range(start: {StartTime.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ssZ}, stop: {EndTime.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ssZ}) " +
                           $"|> filter(fn: (r) => r[\"DataName\"] == \"{VariableName}\") " +
                           $"|> sort(columns: [\"_time\"], desc: false)";

                var results = await _influxDbService.QueryDataAsync(query);

                QueryResults = new List<VariableValueDto>();
                foreach (var result in results)
                {
                    // 由于现在返回的是字典，我们需要使用索引器来访问值
                    // Handle NodaTime.Instant conversion to DateTime
                    DateTime? time = null;
                    if (result.ContainsKey("Time"))
                    {
                        var timeValue = result["Time"];
                        if (timeValue is DateTime dt)
                        {
                            time = dt;
                        }
                        else if (timeValue is NodaTime.Instant instant)
                        {
                            time = instant.ToDateTimeOffset().LocalDateTime;
                        }
                        else if (timeValue != null && timeValue.GetType() == typeof(DateTime?))
                        {
                            time = (DateTime?)timeValue;
                        }
                        else if (timeValue != null && timeValue.GetType() == typeof(DateTime))
                        {
                            time = (DateTime)timeValue;
                        }
                    }

                    var value = result.ContainsKey("Value") ? result["Value"] : null;
                    var field = result.ContainsKey("Field") ? result["Field"] : null;

                    QueryResults.Add(new VariableValueDto
                    {
                        VariableName = VariableName,
                        Value = value?.ToString() ?? "N/A",
                        Time = time ?? DateTime.MinValue
                    });
                }

               
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"查询失败: {ex.Message}";
                return Page();
            }

            return Page();
        }
    }
}