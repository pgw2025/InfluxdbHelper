using InfluxdbHelper.DTOs;
using InfluxdbHelper.Services;
using System.Linq;

namespace InfluxdbHelper.Services
{
    public interface IStatisticsService
    {
        Task<DataStatisticsDto> GetTotalDataCountAsync(DateTime startTime, DateTime endTime);
        Task<List<VariableStatisticsDto>> GetVariableCountsAsync(DateTime startTime, DateTime endTime);
        Task<List<VariableValueDto>> GetVariableHistoryAsync(string variableName, DateTime startTime, DateTime endTime);
    }

    public class StatisticsService : IStatisticsService
    {
        private readonly IInfluxDBService _influxDbService;
        private readonly IConfiguration _configuration;
        private readonly string _bucket;

        public StatisticsService(IInfluxDBService influxDbService, IConfiguration configuration)
        {
            _influxDbService = influxDbService;
            _configuration = configuration;
            _bucket = _configuration.GetSection("InfluxDBConfig").GetValue<string>("Bucket") ?? string.Empty;
        }

        public async Task<DataStatisticsDto> GetTotalDataCountAsync(DateTime startTime, DateTime endTime)
        {
            // 获取各个变量的计数，然后求和作为总数据条数
            var variableCounts = await GetVariableCountsAsync(startTime, endTime);
            int totalCount = variableCounts.Sum(v => v.Count);

            return new DataStatisticsDto
            {
                TotalCount = totalCount,
                StartTime = startTime,
                EndTime = endTime,
                Period = GetPeriodByRange(startTime, endTime)
            };
        }

        public async Task<List<VariableStatisticsDto>> GetVariableCountsAsync(DateTime startTime, DateTime endTime)
        {
            // 查询所有DataName标签的计数
            var query = $"from(bucket: \"{_bucket}\") |> range(start: {startTime:yyyy-MM-ddTHH:mm:ssZ}, stop: {endTime:yyyy-MM-ddTHH:mm:ssZ}) |> group(columns: [\"DataName\"]) |> count(column: \"_value\")";

            var result = await _influxDbService.QueryDataAsync(query);

            var variableStats = new List<VariableStatisticsDto>();

            foreach (var record in result)
            {
                var dataName = record.Tags.ContainsKey("DataName") ? record.Tags["DataName"].ToString() : "unknown";

                if (int.TryParse(record.Value?.ToString(), out int count))
                {
                    variableStats.Add(new VariableStatisticsDto
                    {
                        VariableName = dataName,
                        Count = count,
                        StartTime = startTime,
                        EndTime = endTime,
                        Period = GetPeriodByRange(startTime, endTime)
                    });
                }
            }

            // 按数据条数降序排列
            return variableStats.OrderByDescending(v => v.Count).ToList();
        }

        public async Task<List<VariableValueDto>> GetVariableHistoryAsync(string variableName, DateTime startTime, DateTime endTime)
        {
            var query = $"from(bucket: \"{_bucket}\") |> range(start: {startTime:yyyy-MM-ddTHH:mm:ssZ}, stop: {endTime:yyyy-MM-ddTHH:mm:ssZ}) |> filter(fn: (r) => r[\"_field\"] == \"{variableName}\") |> yield(name: \"values\")";
            
            var result = await _influxDbService.QueryDataAsync(query);
            
            var variableValues = new List<VariableValueDto>();
            
            foreach (var record in result)
            {
                variableValues.Add(new VariableValueDto
                {
                    VariableName = record.Field,
                    Value = record.Value,
                    Time = record.Time?.ToLocalTime() ?? DateTime.MinValue
                });
            }

            return variableValues;
        }

        private string GetPeriodByRange(DateTime start, DateTime end)
        {
            var diff = end - start;
            
            if (diff.TotalHours <= 24)
                return "day";
            else if (diff.TotalDays <= 7)
                return "week";
            else
                return "month";
        }
    }
}