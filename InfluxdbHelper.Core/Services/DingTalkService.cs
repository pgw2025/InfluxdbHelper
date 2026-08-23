using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace InfluxdbHelper.Services
{
    public interface IDingTalkService
    {
        Task<bool> SendTextMessageAsync(string webhookUrl, string secret, string message);
        Task<bool> SendMarkdownMessageAsync(string webhookUrl, string secret, string title, string markdownContent);
    }

    public class DingTalkService : IDingTalkService
    {
        private readonly HttpClient _httpClient;

        public DingTalkService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<bool> SendTextMessageAsync(string webhookUrl, string secret, string message)
        {
            try
            {
                var payload = new
                {
                    msgtype = "text",
                    text = new { content = message }
                };

                var finalUrl = webhookUrl;
                // 如果提供了密钥，则添加签名参数
                if (!string.IsNullOrEmpty(secret))
                {
                    finalUrl = AddSignatureToUrl(webhookUrl, secret);
                }

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

                var response = await _httpClient.PostAsync(finalUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                // 检查响应内容以确认是否发送成功
                dynamic responseObj = JsonConvert.DeserializeObject(responseContent);
                return responseObj != null && responseObj.errcode?.Value == 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送钉钉消息失败: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendMarkdownMessageAsync(string webhookUrl, string secret, string title, string markdownContent)
        {
            try
            {
                var payload = new
                {
                    msgtype = "markdown",
                    markdown = new { title = title, text = markdownContent }
                };

                var finalUrl = webhookUrl;
                // 如果提供了密钥，则添加签名参数
                if (!string.IsNullOrEmpty(secret))
                {
                    finalUrl = AddSignatureToUrl(webhookUrl, secret);
                }

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

                var response = await _httpClient.PostAsync(finalUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                // 检查响应内容以确认是否发送成功
                dynamic responseObj = JsonConvert.DeserializeObject(responseContent);
                return responseObj != null && responseObj.errcode?.Value == 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送钉钉Markdown消息失败: {ex.Message}");
                return false;
            }
        }

        private string AddSignatureToUrl(string webhookUrl, string secret)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var secretEnc = Encoding.UTF8.GetBytes(secret);
            var stringToSign = timestamp + "\n" + secret;
            var bytesToSign = Encoding.UTF8.GetBytes(stringToSign);

            using (var hmac = new HMACSHA256(secretEnc))
            {
                var hash = hmac.ComputeHash(bytesToSign);
                var signature = Convert.ToBase64String(hash);
                var encodedSignature = System.Uri.EscapeDataString(signature);
                var encodedSecret = System.Uri.EscapeDataString(secret);

                return $"{webhookUrl}&timestamp={timestamp}&sign={encodedSignature}";
            }
        }
    }
}