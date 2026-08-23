using InfluxdbHelper.Api.Infrastructure;
using InfluxdbHelper.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InfluxdbHelper.Api.Controllers
{
    /// <summary>
    /// 字段名联想（自旧项目 VariablesController 迁移，增加鉴权）。
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class VariablesController : ControllerBase
    {
        private readonly IInfluxDBService _influxDbService;

        public VariablesController(IInfluxDBService influxDbService)
        {
            _influxDbService = influxDbService;
        }

        [HttpGet("autocomplete")]
        public async Task<IActionResult> GetVariableSuggestions([FromQuery] string? query)
        {
            try
            {
                var allFieldNames = await _influxDbService.GetFieldNamesAsync();

                IEnumerable<string> suggestions = allFieldNames
                    .Where(name => !string.IsNullOrEmpty(name))
                    .Distinct();

                if (!string.IsNullOrWhiteSpace(query))
                {
                    var q = query.ToLowerInvariant();
                    suggestions = suggestions.Where(name => name.ToLowerInvariant().Contains(q));
                }

                return Ok(ApiResponse.Ok(suggestions
                    .OrderBy(name => name)
                    .Take(10)
                    .ToArray()));
            }
            catch (Exception ex)
            {
                // 与旧版行为一致：联想失败不影响前端使用，返回空数组
                Console.WriteLine($"获取变量建议时出错: {ex.Message}");
                return Ok(ApiResponse.Ok(Array.Empty<string>()));
            }
        }
    }
}
