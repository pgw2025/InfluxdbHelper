namespace InfluxdbHelper.Models
{
    public class InfluxDBConfig
    {
        public string Url { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string Org { get; set; } = string.Empty;
        public string Bucket { get; set; } = string.Empty;
        /// <summary>
        /// InfluxDB 引擎数据目录（engine path）。仅当本 API 与 InfluxDB 同机时才可用于统计占用空间；留空则无法获取。
        /// </summary>
        public string EnginePath { get; set; } = string.Empty;
    }
}