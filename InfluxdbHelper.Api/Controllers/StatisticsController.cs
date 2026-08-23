using InfluxdbHelper.Api.Dtos;
using InfluxdbHelper.Api.Infrastructure;
using InfluxdbHelper.DTOs;
using InfluxdbHelper.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InfluxdbHelper.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StatisticsController : ControllerBase
    {
        private readonly IStatisticsService _statisticsService;

        public StatisticsController(IStatisticsService statisticsService)
        {
            _statisticsService = statisticsService;
        }

        /// <summary>
        /// 统计汇总：总量 + 各变量分布（对应旧版 Statistics/Index OnGet）。
        /// period: day / yesterday / daybefore / week / month / custom。
        /// </summary>
        [HttpGet("summary")]
        public async Task<IActionResult> Summary(
            [FromQuery] string period = "day",
            [FromQuery] DateTime? start = null,
            [FromQuery] DateTime? end = null)
        {
            var (startTime, endTime) = TimeRangeHelper.Calculate(period, start, end);

            var total = await _statisticsService.GetTotalDataCountAsync(startTime, endTime);
            var variables = await _statisticsService.GetVariableCountsAsync(startTime, endTime);

            return Ok(ApiResponse.Ok(new
            {
                period,
                startTime,
                endTime,
                total,
                variables
            }));
        }

        /// <summary>
        /// 变量历史分页查询（对应旧版 OnPostViewHistory / OnPostChangePage，分页移到服务端）。
        /// </summary>
        [HttpGet("history")]
        public async Task<IActionResult> History(
            [FromQuery] string variableName,
            [FromQuery] DateTime? start = null,
            [FromQuery] DateTime? end = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            if (string.IsNullOrWhiteSpace(variableName))
            {
                return Ok(ApiResponse.Fail(3001, "变量名不能为空"));
            }
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;
            if (pageSize > 500) pageSize = 500;

            // 未指定时间范围时默认最近 24 小时（与旧版 VariableHistory 行为一致）
            var endTime = end ?? DateTime.Now;
            var startTime = start ?? endTime.AddHours(-24);

            var all = await _statisticsService.GetVariableHistoryAsync(variableName, startTime, endTime);
            var ordered = all.OrderByDescending(h => h.Time).ToList();

            var result = new PagedResult<VariableValueDto>
            {
                Items = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
                Total = ordered.Count,
                Page = page,
                PageSize = pageSize
            };

            return Ok(ApiResponse.Ok(new
            {
                variableName,
                startTime,
                endTime,
                result
            }));
        }
    }
}
