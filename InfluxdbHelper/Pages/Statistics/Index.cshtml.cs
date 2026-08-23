using InfluxdbHelper.DTOs;
using InfluxdbHelper.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InfluxdbHelper.Pages.Statistics
{
    public class IndexModel : PageModel
    {
        private readonly IStatisticsService _statisticsService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(IStatisticsService statisticsService, ILogger<IndexModel> logger)
        {
            _statisticsService = statisticsService;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public string Period { get; set; } = "day"; // 默认为天

        [BindProperty(SupportsGet = true)]
        public DateTime? CustomStartTime { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? CustomEndTime { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1; // 分页页码

        public DataStatisticsDto? TotalStatistics { get; set; }
        public List<VariableStatisticsDto>? VariableStatistics { get; set; }
        public List<VariableValueDto>? VariableHistory { get; set; }
        public string SelectedVariable { get; set; } = string.Empty;
        public bool IsHistoryQuery { get; set; } = false;
        public bool ConnectionOk { get; set; } = false;
        public int TotalHistoryCount { get; set; } = 0; // 总历史记录数
        public int PageSize { get; set; } = 50; // 每页显示数量
        public int TotalPages { get; set; } = 0; // 总页数

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // 检查InfluxDB连接
                ConnectionOk = true;

                // 根据时间段计算起止时间
                var (startTime, endTime) = CalculateTimeRange(Period, CustomStartTime, CustomEndTime);

                // 获取总数据统计
                TotalStatistics = await _statisticsService.GetTotalDataCountAsync(startTime, endTime);

                // 获取各变量统计数据
                VariableStatistics = await _statisticsService.GetVariableCountsAsync(startTime, endTime);

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取统计数据时发生错误");
                ConnectionOk = false;
                TempData["ErrorMessage"] = "无法连接到InfluxDB或查询失败：" + ex.Message;
                return Page();
            }
        }

        public async Task<ActionResult> OnPostViewHistoryAsync(string variableName)
        {
            try
            {
                // 检查InfluxDB连接
                ConnectionOk = true;
                SelectedVariable = variableName;
                IsHistoryQuery = true;
                PageNumber = 1; // 重置为第一页

                // 使用当前时间段来查询历史数据
                var (startTime, endTime) = CalculateTimeRange(Period, CustomStartTime, CustomEndTime);

                // 查询变量历史数据
                var allHistory = await _statisticsService.GetVariableHistoryAsync(variableName, startTime, endTime);

                // 计算分页信息
                TotalHistoryCount = allHistory.Count;
                TotalPages = (int)Math.Ceiling((double)TotalHistoryCount / PageSize);

                // 获取当前页的数据
                VariableHistory = allHistory
                    .OrderByDescending(h => h.Time) // 按时间倒序排列
                    .Skip((PageNumber - 1) * PageSize) // 跳过前面的记录
                    .Take(PageSize) // 取当前页的记录
                    .ToList();

                // 同时获取基本统计数据用于显示
                TotalStatistics = await _statisticsService.GetTotalDataCountAsync(startTime, endTime);
                VariableStatistics = await _statisticsService.GetVariableCountsAsync(startTime, endTime);

                // 保持当前时间段设置，而不是重置为"day"
                // Period = "day"; // 注释掉这行，保持当前时间段

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取变量历史数据时发生错误，变量名: {VariableName}", variableName);
                ConnectionOk = false;
                TempData["ErrorMessage"] = "无法连接到InfluxDB或查询失败：" + ex.Message;

                // 即使出错也要设置变量名，以便用户可以尝试其他操作
                SelectedVariable = variableName;
                IsHistoryQuery = true;

                return Page();
            }
        }

        public async Task<IActionResult> OnPostChangePeriodAsync(string period)
        {
            Period = period;
            return await OnGetAsync();
        }

        public async Task<ActionResult> OnPostChangePageAsync(string variableName, int pageNumber)
        {
            try
            {
                // 检查InfluxDB连接
                ConnectionOk = true;
                SelectedVariable = variableName;
                PageNumber = pageNumber;
                IsHistoryQuery = true;

                // 使用当前时间段来查询历史数据
                var (startTime, endTime) = CalculateTimeRange(Period, CustomStartTime, CustomEndTime);

                // 查询变量历史数据
                var allHistory = await _statisticsService.GetVariableHistoryAsync(variableName, startTime, endTime);

                // 计算分页信息
                TotalHistoryCount = allHistory.Count;
                TotalPages = (int)Math.Ceiling((double)TotalHistoryCount / PageSize);

                // 确保页码在有效范围内
                if (PageNumber < 1) PageNumber = 1;
                if (PageNumber > TotalPages) PageNumber = TotalPages;

                // 获取当前页的数据
                VariableHistory = allHistory
                    .OrderByDescending(h => h.Time) // 按时间倒序排列
                    .Skip((PageNumber - 1) * PageSize) // 跳过前面的记录
                    .Take(PageSize) // 取当前页的记录
                    .ToList();

                // 同时获取基本统计数据用于显示
                TotalStatistics = await _statisticsService.GetTotalDataCountAsync(startTime, endTime);
                VariableStatistics = await _statisticsService.GetVariableCountsAsync(startTime, endTime);

                // 保持当前时间段设置
                // Period = "day"; // 注释掉这行，保持当前时间段

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "切换历史数据页面时发生错误，变量名: {VariableName}, 页码: {PageNumber}", variableName, pageNumber);
                ConnectionOk = false;
                TempData["ErrorMessage"] = "无法连接到InfluxDB或查询失败：" + ex.Message;
                return Page();
            }
        }

        private (DateTime start, DateTime end) CalculateTimeRange(string period, DateTime? customStartTime = null, DateTime? customEndTime = null)
        {
            var now = DateTime.Now;
            DateTime start, end;

            // 如果是自定义时间范围
            if (period == "custom" && customStartTime.HasValue && customEndTime.HasValue)
            {
                start = customStartTime.Value;
                end = customEndTime.Value;

                // 确保开始时间不晚于结束时间
                if (start > end)
                {
                    var temp = start;
                    start = end;
                    end = temp;
                }

                return (start, end);
            }

            switch (period.ToLower())
            {
                case "yesterday": // 昨日
                    start = now.Date.AddDays(-1); // 昨天开始
                    end = start.AddDays(1).AddTicks(-1); // 昨天结束
                    break;
                case "daybefore": // 前日
                    start = now.Date.AddDays(-2); // 前天开始
                    end = start.AddDays(1).AddTicks(-1); // 前天结束
                    break;
                case "day":
                    start = now.Date; // 当天开始
                    end = now;
                    break;
                case "week":
                    // 计算本周周一
                    var daysFromMonday = (int)now.DayOfWeek - 1;
                    if (daysFromMonday < 0) daysFromMonday = 6; // 如果是周日，则距离周一6天
                    start = now.Date.AddDays(-daysFromMonday);
                    end = now;
                    break;
                case "month":
                    start = new DateTime(now.Year, now.Month, 1); // 当月第一天
                    end = now;
                    break;
                default:
                    start = now.Date; // 默认为当天
                    end = now;
                    break;
            }

            return (start, end);
        }
    }
}