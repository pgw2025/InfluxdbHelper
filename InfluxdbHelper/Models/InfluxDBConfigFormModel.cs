using System.ComponentModel.DataAnnotations;

namespace InfluxdbHelper.Models
{
    public class InfluxDBConfigFormModel
    {
        [Required(ErrorMessage = "URL是必需的")]
        public string Url { get; set; } = "http://localhost:8086";

        [Required(ErrorMessage = "Token是必需的")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "Org是必需的")]
        public string Org { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bucket是必需的")]
        public string Bucket { get; set; } = string.Empty;

        public string DingTalkWebhookUrl { get; set; } = string.Empty;

        public string DingTalkSecret { get; set; } = string.Empty;

        public bool DingTalkEnabled { get; set; } = true;

        public bool SaveToConfigFile { get; set; } = false;
    }
}