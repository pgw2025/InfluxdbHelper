using System.ComponentModel.DataAnnotations;

namespace InfluxdbHelper.Models
{
    public class InfluxDBConfigFormModel
    {
        [Required(ErrorMessage = "URL是必需的")]
        public string Url { get; set; } = "http://localhost:8086";

        [Required(ErrorMessage = "Token是必需的")]
        public string Token { get; set; } = string.Empty;

        // 用于显示的Token值（部分隐藏）
        public string DisplayToken
        {
            get
            {
                if (string.IsNullOrEmpty(Token))
                    return string.Empty;

                if (Token.Length <= 8)
                    return new string('*', Token.Length);

                // 显示前4位和后4位，中间用...代替
                return Token.Substring(0, 4) + "..." + Token.Substring(Token.Length - 4);
            }
        }

        [Required(ErrorMessage = "Org是必需的")]
        public string Org { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bucket是必需的")]
        public string Bucket { get; set; } = string.Empty;

        public string DingTalkWebhookUrl { get; set; } = string.Empty;

        public string DingTalkSecret { get; set; } = string.Empty;

        // 用于显示的钉钉机器人密钥值（部分隐藏）
        public string DisplayDingTalkSecret
        {
            get
            {
                if (string.IsNullOrEmpty(DingTalkSecret))
                    return string.Empty;

                if (DingTalkSecret.Length <= 8)
                    return new string('*', DingTalkSecret.Length);

                // 显示前4位和后4位，中间用...代替
                return DingTalkSecret.Substring(0, 4) + "..." + DingTalkSecret.Substring(DingTalkSecret.Length - 4);
            }
        }

        public bool DingTalkEnabled { get; set; } = true;

        public bool SaveToConfigFile { get; set; } = false;
    }
}