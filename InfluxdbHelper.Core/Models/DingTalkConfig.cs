namespace InfluxdbHelper.Models
{
    public class DingTalkConfig
    {
        public string WebhookUrl { get; set; } = string.Empty;
        public string Secret { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
    }
}