using InfluxdbHelper.DTOs;
using InfluxdbHelper.Services;
using System.Linq;
using NodaTime;

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
                // Convert dynamic record to dictionary to access properties
                var recordDict = (IDictionary<string, object>)record;

                // Try to get DataName from the record - it might be stored in different ways
                string dataName = "unknown";

                // Check if there's a DataName key in the record
                if (recordDict.ContainsKey("DataName"))
                {
                    dataName = recordDict["DataName"]?.ToString() ?? "unknown";
                }
                // Otherwise, check if there's a tag field that contains DataName
                else if (recordDict.ContainsKey("tag") && recordDict["tag"] is IDictionary<string, object> tagDict && tagDict.ContainsKey("DataName"))
                {
                    dataName = tagDict["DataName"]?.ToString() ?? "unknown";
                }
                // As a fallback, check for other common keys that might contain the variable name
                else if (recordDict.ContainsKey("name"))
                {
                    dataName = recordDict["name"]?.ToString() ?? "unknown";
                }
                else if (recordDict.ContainsKey("_measurement"))
                {
                    dataName = recordDict["_measurement"]?.ToString() ?? "unknown";
                }
                else if (recordDict.ContainsKey("result"))
                {
                    dataName = recordDict["result"]?.ToString() ?? "unknown";
                }

                // Get the count value
                object countValue = null;
                if (recordDict.ContainsKey("_value"))
                {
                    countValue = recordDict["_value"];
                }
                else if (recordDict.ContainsKey("Value"))
                {
                    countValue = recordDict["Value"];
                }
                else if (recordDict.ContainsKey("value"))
                {
                    countValue = recordDict["value"];
                }

                if (countValue != null && int.TryParse(countValue.ToString(), out int count))
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
            var variableValues = new List<VariableValueDto>();

            // First, try querying by DataName tag (as mentioned by user this is the correct field)
            var query = $"from(bucket: \"{_bucket}\") |> range(start: {startTime:yyyy-MM-ddTHH:mm:ssZ}, stop: {endTime:yyyy-MM-ddTHH:mm:ssZ}) |> filter(fn: (r) => r[\"DataName\"] == \"{variableName}\") |> yield(name: \"values\")";

            var result = await _influxDbService.QueryDataAsync(query);

            foreach (var record in result)
            {
                // Convert dynamic record to dictionary to access properties
                var recordDict = (IDictionary<string, object>)record;

                // Extract values from the record dictionary
                object timeValue = null;
                if (recordDict.ContainsKey("Time"))
                {
                    timeValue = recordDict["Time"];
                }
                else if (recordDict.ContainsKey("_time"))
                {
                    timeValue = recordDict["_time"];
                }

                object value = null;
                if (recordDict.ContainsKey("Value"))
                {
                    value = recordDict["Value"];
                }
                else if (recordDict.ContainsKey("_value"))
                {
                    value = recordDict["_value"];
                }
                else if (recordDict.ContainsKey("value"))
                {
                    value = recordDict["value"];
                }

                object dataName = null;
                if (recordDict.ContainsKey("DataName"))
                {
                    dataName = recordDict["DataName"];
                }
                else if (recordDict.ContainsKey("Field"))
                {
                    dataName = recordDict["Field"];
                }
                

                // Convert time to DateTime, handling potential nulls and NodaTime.Instant
                DateTime time = DateTime.MinValue;
                if (timeValue != null)
                {
                    if (timeValue is DateTime dt)
                    {
                        time = dt.ToLocalTime();
                    }
                    else if (timeValue is NodaTime.Instant instant)
                    {
                        time = instant.ToDateTimeOffset().LocalDateTime;
                    }
                    else if (DateTime.TryParse(timeValue.ToString(), out DateTime parsedTime))
                    {
                        time = parsedTime.ToLocalTime();
                    }
                }

                variableValues.Add(new VariableValueDto
                {
                    VariableName = (dataName?.ToString()) ?? variableName,
                    Value = value,
                    Time = time
                });
            }

            // If no results found with DataName, try alternative approaches
            if (variableValues.Count == 0)
            {
                // Try filtering by _field as a fallback
                var fieldQuery = $"from(bucket: \"{_bucket}\") |> range(start: {startTime:yyyy-MM-ddTHH:mm:ssZ}, stop: {endTime:yyyy-MM-ddTHH:mm:ssZ}) |> filter(fn: (r) => r[\"_field\"] == \"{variableName}\") |> yield(name: \"values\")";

                var fieldResult = await _influxDbService.QueryDataAsync(fieldQuery);

                foreach (var record in fieldResult)
                {
                    // Convert dynamic record to dictionary to access properties
                    var recordDict = (IDictionary<string, object>)record;

                    // Extract values from the record dictionary
                    object timeValue = null;
                    if (recordDict.ContainsKey("Time"))
                    {
                        timeValue = recordDict["Time"];
                    }
                    else if (recordDict.ContainsKey("_time"))
                    {
                        timeValue = recordDict["_time"];
                    }

                    object value = null;
                    if (recordDict.ContainsKey("Value"))
                    {
                        value = recordDict["Value"];
                    }
                    else if (recordDict.ContainsKey("_value"))
                    {
                        value = recordDict["_value"];
                    }
                    else if (recordDict.ContainsKey("value"))
                    {
                        value = recordDict["value"];
                    }

                    // Convert time to DateTime, handling potential nulls and NodaTime.Instant
                    DateTime time = DateTime.MinValue;
                    if (timeValue != null)
                    {
                        if (timeValue is DateTime dt)
                        {
                            time = dt.ToLocalTime();
                        }
                        else if (timeValue is NodaTime.Instant instant)
                        {
                            time = instant.ToDateTimeOffset().LocalDateTime;
                        }
                        else if (DateTime.TryParse(timeValue.ToString(), out DateTime parsedTime))
                        {
                            time = parsedTime.ToLocalTime();
                        }
                    }

                    variableValues.Add(new VariableValueDto
                    {
                        VariableName = variableName,
                        Value = value,
                        Time = time
                    });
                }
            }

            // If still no results, try other common tag names that might contain the variable name
            if (variableValues.Count == 0)
            {
                // Try variable or name tags
                var altQuery = $"from(bucket: \"{_bucket}\") |> range(start: {startTime:yyyy-MM-ddTHH:mm:ssZ}, stop: {endTime:yyyy-MM-ddTHH:mm:ssZ}) |> filter(fn: (r) => r[\"variable\"] == \"{variableName}\" or r[\"name\"] == \"{variableName}\") |> yield(name: \"values\")";

                var altResult = await _influxDbService.QueryDataAsync(altQuery);

                foreach (var record in altResult)
                {
                    // Convert dynamic record to dictionary to access properties
                    var recordDict = (IDictionary<string, object>)record;

                    // Extract values from the record dictionary
                    object timeValue = null;
                    if (recordDict.ContainsKey("Time"))
                    {
                        timeValue = recordDict["Time"];
                    }
                    else if (recordDict.ContainsKey("_time"))
                    {
                        timeValue = recordDict["_time"];
                    }

                    object value = null;
                    if (recordDict.ContainsKey("Value"))
                    {
                        value = recordDict["Value"];
                    }
                    else if (recordDict.ContainsKey("_value"))
                    {
                        value = recordDict["_value"];
                    }
                    else if (recordDict.ContainsKey("value"))
                    {
                        value = recordDict["value"];
                    }

                    // Convert time to DateTime, handling potential nulls and NodaTime.Instant
                    DateTime time = DateTime.MinValue;
                    if (timeValue != null)
                    {
                        if (timeValue is DateTime dt)
                        {
                            time = dt.ToLocalTime();
                        }
                        else if (timeValue is NodaTime.Instant instant)
                        {
                            time = instant.ToDateTimeOffset().LocalDateTime;
                        }
                        else if (DateTime.TryParse(timeValue.ToString(), out DateTime parsedTime))
                        {
                            time = parsedTime.ToLocalTime();
                        }
                    }

                    variableValues.Add(new VariableValueDto
                    {
                        VariableName = variableName,
                        Value = value,
                        Time = time
                    });
                }
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