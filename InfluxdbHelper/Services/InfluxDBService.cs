using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Core;
using InfluxdbHelper.Models;

namespace InfluxdbHelper.Services
{
    public interface IInfluxDBService
    {
        Task<List<dynamic>> QueryDataAsync(string query);
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
                        var data = new
                        {
                            Time = record.GetTime(),
                            Measurement = record.GetMeasurement(),
                            Field = record.GetField(),
                            Value = record.GetValue(),
                            Tags = record.Values
                                .Where(v => v.Key.StartsWith("_") == false &&
                                          v.Key != "result" &&
                                          v.Key != "table" &&
                                          v.Key != record.GetField())
                                .ToDictionary(v => v.Key, v => v.Value)
                        };
                        results.Add(data);
                    }
                }

                return results;
            }
            catch (Exception ex)
            {
                throw new Exception($"查询InfluxDB时发生错误: {ex.Message}", ex);
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