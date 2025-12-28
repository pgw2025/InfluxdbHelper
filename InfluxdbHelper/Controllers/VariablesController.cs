using Microsoft.AspNetCore.Mvc;
using InfluxdbHelper.Services;
using InfluxdbHelper.Models;
using System.Text.Json;

namespace InfluxdbHelper.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VariablesController : ControllerBase
    {
        private readonly IInfluxDBService _influxDbService;
        private readonly IConfiguration _configuration;

        public VariablesController(IInfluxDBService influxDbService, IConfiguration configuration)
        {
            _influxDbService = influxDbService;
            _configuration = configuration;
        }

        [HttpGet("autocomplete")]
        public async Task<IActionResult> GetVariableSuggestions([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                // 如果没有查询参数，返回最近使用的一些字段名
                try
                {
                    var allFieldNames = await _influxDbService.GetFieldNamesAsync();
                    var recentSuggestions = allFieldNames
                        .Where(name => !string.IsNullOrEmpty(name))
                        .Distinct()
                        .Take(10) // 限制返回结果数量
                        .OrderBy(name => name)
                        .ToArray();

                    return Ok(recentSuggestions);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"获取变量建议时出错: {ex.Message}");
                    return Ok(new string[0]);
                }
            }

            try
            {
                // 获取所有字段名称
                var allFieldNames = await _influxDbService.GetFieldNamesAsync();

                // 过滤出包含查询字符串的字段名（不区分大小写）
                var suggestions = allFieldNames
                    .Where(name => !string.IsNullOrEmpty(name) &&
                                  name.ToLower().Contains(query.ToLower()))
                    .Distinct()
                    .Take(10) // 限制返回结果数量
                    .OrderBy(name => name)
                    .ToArray();

                return Ok(suggestions);
            }
            catch (Exception ex)
            {
                // 记录错误但返回空数组，避免前端出错
                Console.WriteLine($"获取变量建议时出错: {ex.Message}");
                return Ok(new string[0]);
            }
        }
    }
}