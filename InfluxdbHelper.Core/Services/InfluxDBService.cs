using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Core;
using InfluxdbHelper.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http.Headers;
using System.Text.Json;
using System.IO;

namespace InfluxdbHelper.Services
{
    public interface IInfluxDBService
    {
        Task<List<dynamic>> QueryDataAsync(string query);
        Task<List<string>> GetFieldNamesAsync();
        Task<bool> PingAsync();
        Task<bool> ReinitializeClientAsync();
        /// <summary>Bucket 内总数据点数量（5 分钟缓存，避免全表扫描）。</summary>
        Task<long> GetTotalPointCountAsync();
        /// <summary>InfluxDB 服务启动时间（取自 /ready 的 started 字段，本地时区）。取不到返回 null。</summary>
        Task<DateTime?> GetStartedAtAsync();
        /// <summary>InfluxDB 引擎目录占用字节数。未配置/不可用时返回 -1。</summary>
        Task<long> GetStorageSizeBytesAsync();
    }

    public class InfluxDBService : IInfluxDBService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private InfluxDBClient _client;
        private string _org;
        private string _bucket;
        private string _url;
        private string _token;
        private string _enginePath;

        public InfluxDBService(IConfiguration configuration, IHttpClientFactory httpClientFactory, IMemoryCache cache)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            InitializeClient();
        }

        private void InitializeClient()
        {
            var config = _configuration.GetSection("InfluxDBConfig").Get<InfluxDBConfig>();

            _url = config.Url ?? string.Empty;
            _token = config.Token ?? string.Empty;
            _enginePath = config.EnginePath ?? string.Empty;
            _org = string.IsNullOrEmpty(config.Org) ? "" : config.Org;
            _bucket = string.IsNullOrEmpty(config.Bucket) ? "" : config.Bucket;

            if (!string.IsNullOrEmpty(config.Token))
            {
                // 如果提供了token，使用两个参数的构造函数
                _client = new InfluxDBClient(config.Url, config.Token);
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
            }
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

        public async Task<long> GetTotalPointCountAsync()
        {
            const string cacheKey = "influx_total_point_count";
            if (_cache.TryGetValue<long>(cacheKey, out var cached))
            {
                return cached;
            }

            // 全桶扫描点数：性能开销大，故加 5 分钟缓存
            var query = $"from(bucket: \"{_bucket}\") |> range(start: 0) |> group() |> count(column: \"_value\")";
            var result = await QueryDataAsync(query);

            long total = 0;
            foreach (var record in result)
            {
                var dict = (System.Collections.Generic.IDictionary<string, object>)record;
                object? raw = null;
                if (dict.TryGetValue("_value", out var v1)) raw = v1;
                else if (dict.TryGetValue("Value", out var v2)) raw = v2;
                if (raw != null && long.TryParse(raw.ToString(), out var c))
                {
                    total += c;
                }
            }

            _cache.Set(cacheKey, total, TimeSpan.FromMinutes(5));
            return total;
        }

        public async Task<DateTime?> GetStartedAtAsync()
        {
            if (string.IsNullOrEmpty(_url) || string.IsNullOrEmpty(_token)) return null;

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                using var request = new HttpRequestMessage(HttpMethod.Get, $"{_url.TrimEnd('/')}/ready");
                request.Headers.Authorization = new AuthenticationHeaderValue("Token", _token);

                using var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode) return null;

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("started", out var startedEl)
                    && startedEl.ValueKind == JsonValueKind.String
                    && DateTime.TryParse(startedEl.GetString(), out var started))
                {
                    // InfluxDB 返回 UTC 时间，转本地时区展示
                    return DateTime.SpecifyKind(started, DateTimeKind.Utc).ToLocalTime();
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<long> GetStorageSizeBytesAsync()
        {
            if (string.IsNullOrEmpty(_enginePath) || !Directory.Exists(_enginePath)) return -1;

            try
            {
                return await Task.Run(() =>
                    new DirectoryInfo(_enginePath)
                        .EnumerateFiles("*", SearchOption.AllDirectories)
                        .Sum(f => (long)f.Length));
            }
            catch
            {
                return -1;
            }
        }
    }
}