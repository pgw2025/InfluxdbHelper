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
        /// <summary>将指定时间范围（及可选变量）的数据导出为 CSV 文本。dataName 为空表示全部变量。</summary>
        Task<string> ExportCsvAsync(DateTime start, DateTime stop, string? dataName = null);
        /// <summary>删除指定时间范围（及可选变量）的数据。dataName 为空表示全部变量。返回实际删除的 predicate 描述。</summary>
        Task<string> DeleteAsync(DateTime start, DateTime stop, string? dataName = null);
        /// <summary>返回备份目录（确保已创建）。</summary>
        string GetBackupPath();
        /// <summary>预览指定变量在所选时间范围内的数据：点数、起止时间、当前页抽样行（支持按时间/值排序与分页），用于删除前核对。</summary>
        Task<VariablePreview> PreviewAsync(DateTime start, DateTime stop, string dataName, int page = 1, int pageSize = 20, string sortBy = "time", string sortDir = "asc");
    }

    /// <summary>删除前预览的结构化结果。</summary>
    public class VariablePreview
    {
        public string DataName { get; set; } = string.Empty;
        public long PointCount { get; set; }
        public DateTime? FirstTime { get; set; }
        public DateTime? LastTime { get; set; }
        /// <summary>当前页数据（已排序、已分页）。</summary>
        public List<VariablePreviewSample> Samples { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalPages { get; set; } = 1;
        public string SortBy { get; set; } = "time";
        public string SortDir { get; set; } = "asc";
    }

    /// <summary>预览抽样行。</summary>
    public class VariablePreviewSample
    {
        public DateTime? Time { get; set; }
        public object? Value { get; set; }
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
        private string _backupPath;

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
            _backupPath = config.BackupPath ?? string.Empty;
            if (string.IsNullOrEmpty(_backupPath))
            {
                _backupPath = Path.Combine(AppContext.BaseDirectory, "backups");
            }
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

        /// <summary>
        /// 将指定时间范围（及可选变量）的数据导出为 CSV 文本。
        /// 列：Time, DataName, Value, 以及其余标签列。dataName 为空表示导出全部变量。
        /// </summary>
        public async Task<string> ExportCsvAsync(DateTime start, DateTime stop, string? dataName = null)
        {
            var filter = string.IsNullOrWhiteSpace(dataName)
                ? string.Empty
                : $"|> filter(fn: (r) => r[\"DataName\"] == \"{dataName!.Replace("\"", "\\\"")}\") ";
            var query = $"from(bucket: \"{_bucket}\") " +
                        $"|> range(start: {start:yyyy-MM-ddTHH:mm:ssZ}, stop: {stop:yyyy-MM-ddTHH:mm:ssZ}) " +
                        filter +
                        $"|> sort(columns: [\"_time\"])";

            var records = await QueryDataAsync(query);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Time,DataName,Value");

            foreach (var rec in records)
            {
                var dict = (System.Collections.Generic.IDictionary<string, object>)rec;

                string time = string.Empty;
                if (dict.TryGetValue("Time", out var t) && t != null)
                {
                    time = t is DateTime dt ? dt.ToString("yyyy-MM-dd HH:mm:ss") : t.ToString() ?? string.Empty;
                }
                else if (dict.TryGetValue("_time", out var t2) && t2 != null)
                {
                    time = t2.ToString() ?? string.Empty;
                }

                string dataNameVal = dict.TryGetValue("DataName", out var d) && d != null ? d.ToString() ?? string.Empty : string.Empty;
                string value = dict.TryGetValue("Value", out var v) && v != null
                    ? v.ToString() ?? string.Empty
                    : (dict.TryGetValue("_value", out var v2) && v2 != null ? v2.ToString() ?? string.Empty : string.Empty);

                sb.AppendLine($"{CsvCell(time)},{CsvCell(dataNameVal)},{CsvCell(value)}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// 删除指定时间范围（及可选变量）的数据。返回实际使用的删除 predicate（用于日志）。
        /// 注意：InfluxDB 删除为不可恢复操作，调用方应先备份。
        /// </summary>
        public async Task<string> DeleteAsync(DateTime start, DateTime stop, string? dataName = null)
        {
            string predicate = string.IsNullOrWhiteSpace(dataName)
                ? ""
                : $"DataName=\"{dataName!.Replace("\"", "\\\"")}\"";

            var deleteApi = _client.GetDeleteApi();
            await deleteApi.Delete(start, stop, predicate, _bucket, _org, default);

            return predicate;
        }

        /// <summary>备份目录（确保存在）。</summary>
        public string GetBackupPath()
        {
            if (!Directory.Exists(_backupPath))
            {
                Directory.CreateDirectory(_backupPath);
            }
            return _backupPath;
        }

        /// <summary>
        /// 预览指定变量在所选时间范围内的数据。
        /// 复用 QueryDataAsync 查询全量记录，支持按 time/value 排序、分页，返回当前页用于删除前核对。
        /// </summary>
        public async Task<VariablePreview> PreviewAsync(DateTime start, DateTime stop, string dataName, int page = 1, int pageSize = 20, string sortBy = "time", string sortDir = "asc")
        {
            var safeName = string.IsNullOrWhiteSpace(dataName) ? string.Empty : dataName!.Trim();

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 1;
            if (pageSize > 500) pageSize = 500;
            var dir = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";
            var by = string.Equals(sortBy, "value", StringComparison.OrdinalIgnoreCase) ? "value" : "time";

            // dataName 为空时预览全部变量（不加 DataName 过滤）
            var filter = string.IsNullOrEmpty(safeName)
                ? string.Empty
                : $"|> filter(fn: (r) => r[\"DataName\"] == \"{safeName.Replace("\"", "\\\"")}\") ";
            var query = $"from(bucket: \"{_bucket}\") " +
                        $"|> range(start: {start:yyyy-MM-ddTHH:mm:ssZ}, stop: {stop:yyyy-MM-ddTHH:mm:ssZ}) " +
                        filter +
                        $"|> sort(columns: [\"_time\"])";

            var records = await QueryDataAsync(query);

            // 解析全部记录为（时间, 值）列表
            var all = new List<VariablePreviewSample>();
            DateTime? first = null;
            DateTime? last = null;

            foreach (var rec in records)
            {
                var dict = (System.Collections.Generic.IDictionary<string, object>)rec;

                DateTime? time = null;
                if (dict.TryGetValue("Time", out var t) && t is DateTime dt)
                {
                    time = dt;
                }
                else if (dict.TryGetValue("Time", out var t2) && t2 != null && DateTime.TryParse(t2.ToString(), out var dt2))
                {
                    time = dt2;
                }

                if (time.HasValue)
                {
                    if (first == null || time.Value < first.Value) first = time;
                    if (last == null || time.Value > last.Value) last = time;
                }

                object? value = null;
                if (dict.TryGetValue("Value", out var v) && v != null)
                    value = v;
                else if (dict.TryGetValue("_value", out var v2) && v2 != null)
                    value = v2;

                all.Add(new VariablePreviewSample { Time = time, Value = value });
            }

            // 排序：按时间或按值（数值优先，非数值按字符串）
            var ascending = dir == "asc";
            if (by == "value")
            {
                all.Sort((a, b) =>
                {
                    var ca = ToSortKey(a.Value);
                    var cb = ToSortKey(b.Value);
                    var cmp = ca.CompareTo(cb);
                    return ascending ? cmp : -cmp;
                });
            }
            else
            {
                all.Sort((a, b) =>
                {
                    var ta = a.Time?.Ticks ?? long.MaxValue;
                    var tb = b.Time?.Ticks ?? long.MaxValue;
                    var cmp = ta.CompareTo(tb);
                    return ascending ? cmp : -cmp;
                });
            }

            var total = all.Count;
            var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
            if (page > totalPages) page = totalPages;

            var samples = all
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new VariablePreview
            {
                DataName = string.IsNullOrEmpty(safeName) ? "(全部变量)" : safeName,
                PointCount = total,
                FirstTime = first,
                LastTime = last,
                Samples = samples,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                SortBy = by,
                SortDir = dir
            };
        }

        /// <summary>将值转为可比较的键：数值按 double，非数值按字符串（空值排最后）。</summary>
        private static IComparable ToSortKey(object? value)
        {
            if (value == null) return string.Empty;
            if (value is IConvertible && !(value is string) && TryToDouble(value, out var d))
            {
                return d;
            }
            return value.ToString() ?? string.Empty;
        }

        private static bool TryToDouble(object value, out double result)
        {
            try
            {
                result = Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
                return true;
            }
            catch
            {
                result = 0;
                return false;
            }
        }

        private static string CsvCell(string value)
        {
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            return value;
        }
    }
}