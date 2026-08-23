using InfluxdbHelper.Api.Infrastructure;
using InfluxdbHelper.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;
using System.Text;

namespace InfluxdbHelper.Api.Controllers
{
    /// <summary>
    /// 历史数据维护：导出 CSV、删除（删除前强制备份）。
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MaintenanceController : ControllerBase
    {
        private readonly IInfluxDBService _influxDbService;

        public MaintenanceController(IInfluxDBService influxDbService)
        {
            _influxDbService = influxDbService;
        }

        /// <summary>
        /// 导出指定时间范围（及可选变量）的数据为 CSV 文件下载。
        /// </summary>
        [HttpGet("export")]
        public async Task<IActionResult> Export(
            [FromQuery] DateTime start,
            [FromQuery] DateTime stop,
            [FromQuery] string? dataName = null)
        {
            if (stop <= start)
            {
                return Ok(ApiResponse.Fail(3001, "结束时间必须晚于开始时间"));
            }

            var csv = await _influxDbService.ExportCsvAsync(start, stop, dataName);
            var stamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var safeName = string.IsNullOrWhiteSpace(dataName)
                ? "all"
                : new string(dataName.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-').ToArray());
            var fileName = $"influx-export-{safeName}-{stamp}.csv";

            var bytes = Encoding.UTF8.GetBytes("\uFEFF" + csv); // BOM 便于 Excel 识别 UTF-8
            return File(bytes, "text/csv", fileName);
        }

        /// <summary>
        /// 删除前预览：查询指定变量在所选时间范围内的数据概览与抽样，供用户核对是否确为要删除的目标。
        /// dataName 必填；为空返回错误。
        /// </summary>
        [HttpGet("preview")]
        public async Task<IActionResult> Preview(
            [FromQuery] DateTime start,
            [FromQuery] DateTime stop,
            [FromQuery] string dataName,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string sortBy = "time",
            [FromQuery] string sortDir = "asc")
        {
            if (stop <= start)
            {
                return Ok(ApiResponse.Fail(3001, "结束时间必须晚于开始时间"));
            }
            if (string.IsNullOrWhiteSpace(dataName))
            {
                return Ok(ApiResponse.Fail(3003, "预览必须指定变量名（dataName）"));
            }
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 1;
            if (pageSize > 500) pageSize = 500;

            var preview = await _influxDbService.PreviewAsync(start, stop, dataName.Trim(), page, pageSize, sortBy, sortDir);
            return Ok(ApiResponse.Ok(preview));
        }

        /// <summary>
        /// 删除指定时间范围（及可选变量）的数据。
        /// 安全措施：必须 confirm=true；删除前先导出 CSV 并打包为 zip 备份到 BackupPath；记录操作日志。
        /// </summary>
        [HttpPost("delete")]
        public async Task<IActionResult> Delete([FromBody] DeleteRequest request)
        {
            if (!request.Confirm)
            {
                return Ok(ApiResponse.Fail(3002, "需显式确认（confirm=true）才允许删除"));
            }
            if (request.Stop <= request.Start)
            {
                return Ok(ApiResponse.Fail(3001, "结束时间必须晚于开始时间"));
            }

            // 禁止留空删除全部变量，必须指定具体变量名
            if (string.IsNullOrWhiteSpace(request.DataName))
            {
                return Ok(ApiResponse.Fail(3004, "必须指定变量名才能删除（不支持留空删除全部）"));
            }

            var dataName = request.DataName!.Trim();
            var stamp = DateTime.Now.ToString("yyyyMMddHHmmss");

            // 1) 先导出备份
            var csv = await _influxDbService.ExportCsvAsync(request.Start, request.Stop, dataName);
            var backupDir = _influxDbService.GetBackupPath();
            var safeName = dataName == null
                ? "all"
                : new string(dataName.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-').ToArray());
            var zipName = $"backup-{safeName}-{stamp}.zip";
            var zipPath = Path.Combine(backupDir, zipName);

            using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                var entry = zip.CreateEntry($"export-{safeName}-{stamp}.csv");
                using var entryStream = entry.Open();
                var csvBytes = Encoding.UTF8.GetBytes("\uFEFF" + csv);
                await entryStream.WriteAsync(csvBytes, 0, csvBytes.Length);
            }

            // 2) 执行删除
            var predicate = await _influxDbService.DeleteAsync(request.Start, request.Stop, dataName);

            // 3) 写操作日志
            var logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] user={User.Identity?.Name ?? "unknown"} " +
                          $"action=delete range={request.Start:yyyy-MM-ddTHH:mm:ssZ}~{request.Stop:yyyy-MM-ddTHH:mm:ssZ} " +
                          $"dataName={(dataName ?? "ALL")} predicate=\"{predicate}\" backup={zipName} rows={csv.Split('\n').Length - 1}";
            Console.WriteLine(logLine);
            try
            {
                var logPath = Path.Combine(backupDir, $"maintenance-log-{DateTime.Now:yyyyMMdd}.txt");
                await System.IO.File.AppendAllLinesAsync(logPath, new[] { logLine });
            }
            catch
            {
                // 日志写入失败不影响主流程
            }

            return Ok(ApiResponse.Ok(new
            {
                deleted = true,
                backupFile = zipName
            }, $"删除完成，已备份至 {zipName}"));
        }
    }

    public class DeleteRequest
    {
        public DateTime Start { get; set; }
        public DateTime Stop { get; set; }
        public string? DataName { get; set; }
        public bool Confirm { get; set; }
    }
}
