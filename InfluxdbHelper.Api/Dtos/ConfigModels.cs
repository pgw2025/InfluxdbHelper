using System.ComponentModel.DataAnnotations;

namespace InfluxdbHelper.Api.Dtos
{
    /// <summary>
    /// 配置读取响应（敏感字段脱敏）。
    /// </summary>
    public class ConfigResponse
    {
        public string Url { get; set; } = "http://localhost:8086";
        public string Token { get; set; } = string.Empty;        // 脱敏后，如 abcd...wxyz
        public string Org { get; set; } = string.Empty;
        public string Bucket { get; set; } = string.Empty;
        public string DingTalkWebhookUrl { get; set; } = string.Empty;
        public string DingTalkSecret { get; set; } = string.Empty; // 脱敏后
        public bool DingTalkEnabled { get; set; } = true;
        public int DingTalkSendHour { get; set; } = 9;
        public int DingTalkSendMinute { get; set; } = 0;
        public string DingTalkMessageTemplate { get; set; } = string.Empty;
    }

    /// <summary>
    /// 配置更新请求。Token / DingTalkSecret 留空或传脱敏占位值表示"不修改"。
    /// </summary>
    public class ConfigUpdateRequest
    {
        [Required(ErrorMessage = "URL 是必需的")]
        public string Url { get; set; } = string.Empty;

        public string? Token { get; set; }

        [Required(ErrorMessage = "Org 是必需的")]
        public string Org { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bucket 是必需的")]
        public string Bucket { get; set; } = string.Empty;

        public string DingTalkWebhookUrl { get; set; } = string.Empty;
        public string? DingTalkSecret { get; set; }
        public bool DingTalkEnabled { get; set; } = true;
        public int DingTalkSendHour { get; set; } = 9;
        public int DingTalkSendMinute { get; set; } = 0;
        public string DingTalkMessageTemplate { get; set; } = string.Empty;

        /// <summary>是否持久化到 appsettings.json（只读文件系统/容器环境可置 false，仅本次运行生效需另行处理）</summary>
        public bool Persist { get; set; } = true;
    }

    /// <summary>
    /// 连接测试请求：仅验证，不保存。
    /// </summary>
    public class ConnectionTestRequest
    {
        [Required(ErrorMessage = "URL 是必需的")]
        public string Url { get; set; } = string.Empty;
        public string? Token { get; set; }
        public string Org { get; set; } = string.Empty;
    }

    public class ConfigSaveResult
    {
        public bool Saved { get; set; }
        public bool ConnectionOk { get; set; }
        public string? Error { get; set; }
    }

    public static class SecretsMasker
    {
        /// <summary>保留首尾各 4 位，中间打码；不足 8 位全部打码。</summary>
        public static string Mask(string? secret)
        {
            if (string.IsNullOrEmpty(secret)) return string.Empty;
            return secret.Length <= 8
                ? new string('*', secret.Length)
                : secret.Substring(0, 4) + "..." + secret.Substring(secret.Length - 4);
        }

        /// <summary>前端传回的值是否表示"不修改"（空、纯星号或带 ... 的脱敏占位）。</summary>
        public static bool IsMaskedOrEmpty(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return true;
            return value.Contains('*') || value.Contains("...");
        }
    }
}
