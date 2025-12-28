using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Core;
using InfluxdbHelper.Models;

namespace InfluxdbHelper.Services
{
    public interface IInfluxDBService
    {
        Task<List<dynamic>> QueryDataAsync(string query);
        Task<List<string>> GetFieldNamesAsync();
        Task<bool> PingAsync();
        Task<bool> ReinitializeClientAsync();
    }

    public class InfluxDBService : IInfluxDBService
    {
        private readonly IConfiguration _configuration;
        private InfluxDBClient _client;
        private string _org;
        private string _bucket;

        public InfluxDBService(IConfiguration configuration)
        {
            _configuration = configuration;
            InitializeClient();
        }

        private void InitializeClient()
        {
            var config = _configuration.GetSection("InfluxDBConfig").Get<InfluxDBConfig>();

            if (!string.IsNullOrEmpty(config.Token))
            {
                // 如果提供了token，使用两个参数的构造函数
                _client = new InfluxDBClient(config.Url, config.Token);
                _org = string.IsNullOrEmpty(config.Org) ? "" : config.Org;
            }
            else
            {
                // 如果没有提供token，只使用URL创建客户端
                var options = new InfluxDBClientOptions(config.Url);
                if (!string.IsNullOrEmpty(config.Org))
                {
                    options.Org = config.Org;
                }
                _client = new InfluxDBClient(options);
                _org = string.IsNullOrEmpty(config.Org) ? "" : config.Org;
            }

            _bucket = string.IsNullOrEmpty(config.Bucket) ? "" : config.Bucket;
        }

        public async Task<bool> ReinitializeClientAsync()
        {
            try
            {
                if (_client != null)
                {
                    _client.Dispose();
                }

                InitializeClient();
                return true;
            }
            catch (Exception)
            {
                // 如果重新初始化失败，恢复到之前的客户端
                InitializeClient();
                return false;
            }
        }

        public async Task<List<dynamic>> QueryDataAsync(string query)
        {
            try
            {
                var fluxTables = await _client.GetQueryApi().QueryAsync(query, _org);
                var results = new List<dynamic>();

                foreach (var table in fluxTables)
                {
                    foreach (var record in table.Records)
                    {
                        // 创建一个字典来存储记录的所有属性
                        var recordDict = new System.Collections.Generic.Dictionary<string, object>();

                        // 添加基本属性
                        recordDict["Time"] = record.GetTime();
                        recordDict["Measurement"] = record.GetMeasurement();
                        recordDict["Field"] = record.GetField();
                        recordDict["Value"] = record.GetValue();

                        // 添加所有标签和值
                        foreach (var kvp in record.Values)
                        {
                            if (!kvp.Key.StartsWith("_") &&
                                kvp.Key != "result" &&
                                kvp.Key != "table" &&
                                kvp.Key != record.GetField())
                            {
                                recordDict[kvp.Key] = kvp.Value;
                            }
                        }

                        results.Add(recordDict);
                    }
                }

                return results;
            }
            catch (Exception ex)
            {
                throw new Exception($"查询InfluxDB时发生错误: {ex.Message}", ex);
            }
        }

        public async Task<List<string>> GetFieldNamesAsync()
        {
            try
            {
                // 查询所有字段名称 with a more comprehensive time range
                // Using a larger time range to ensure we get results
                var query = $"from(bucket: \"{_bucket}\") " +
                           $"|> range(start: -1y) " +  // Changed from -30d to -1y to cover more data
                           $"|> group() " +
                           $"|> distinct(column: \"DataName\")";

                var fluxTables = await _client.GetQueryApi().QueryAsync(query, _org);
                var fieldNames = new List<string>();

                foreach (var table in fluxTables)
                {
                    foreach (var record in table.Records)
                    {
                        var fieldValue = record.GetValue();
                        if (fieldValue != null)
                        {
                            var fieldName = fieldValue.ToString();
                            if (!string.IsNullOrEmpty(fieldName))
                            {
                                fieldNames.Add(fieldName);
                            }
                        }
                    }
                }

                return fieldNames.Distinct().ToList();
            }
            catch (Exception ex)
            {
                // If the query with -1y fails, try with a more basic query that might work
                try
                {
                    // Fallback query that might work in more scenarios
                    var fallbackQuery = $"from(bucket: \"{_bucket}\") " +
                                       $"|> range(start: -30d) " +
                                       $"|> limit(n: 100) " +  // Limit to avoid performance issues
                                       $"|> group() " +
                                       $"|> distinct(column: \"DataName\")";

                    var fluxTables = await _client.GetQueryApi().QueryAsync(fallbackQuery, _org);
                    var fieldNames = new List<string>();

                    foreach (var table in fluxTables)
                    {
                        foreach (var record in table.Records)
                        {
                            var fieldValue = record.GetValue();
                            if (fieldValue != null)
                            {
                                var fieldName = fieldValue.ToString();
                                if (!string.IsNullOrEmpty(fieldName))
                                {
                                    fieldNames.Add(fieldName);
                                }
                            }
                        }
                    }

                    return fieldNames.Distinct().ToList();
                }
                catch
                {
                    // If both queries fail, throw the original exception
                    throw new Exception($"获取字段名时发生错误: {ex.Message}", ex);
                }
            }
        }

        public async Task<bool> PingAsync()
        {
            try
            {
                return await _client.PingAsync();
            }
            catch
            {
                return false;
            }
        }
    }
}