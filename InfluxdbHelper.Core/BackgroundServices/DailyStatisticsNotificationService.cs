using InfluxdbHelper.Services;

namespace InfluxdbHelper.BackgroundServices
{
    public class DailyStatisticsNotificationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DailyStatisticsNotificationService> _logger;
        private readonly IConfiguration _configuration;

        public DailyStatisticsNotificationService(IServiceProvider serviceProvider, ILogger<DailyStatisticsNotificationService> logger, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("每日统计数据通知服务已启动");

            // 从配置获取发送时间，默认为上午9点
            var sendHour = _configuration.GetValue<int>("DingTalkConfig:SendHour", 9);
            var sendMinute = _configuration.GetValue<int>("DingTalkConfig:SendMinute", 0);

            // 每天指定时间执行
            var now = DateTime.Now;
            var nextRun = new DateTime(now.Year, now.Month, now.Day, sendHour, sendMinute, 0); // 每天指定时间
            if (now >= nextRun)
            {
                nextRun = nextRun.AddDays(1);
            }

            var delay = nextRun - now;
            if (delay.TotalMilliseconds > 0)
            {
                await Task.Delay(delay, stoppingToken);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SendDailyStatistics();

                    _logger.LogInformation("已发送每日统计数据");

                    // 等待到第二天同一时间
                    await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "发送每日统计数据时发生错误");

                    // 发生错误后等待一段时间再重试，避免无限循环
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }
        }

        private async Task SendDailyStatistics()
        {
            using var scope = _serviceProvider.CreateScope();
            var statisticsService = scope.ServiceProvider.GetRequiredService<IStatisticsService>();
            var dingTalkService = scope.ServiceProvider.GetRequiredService<IDingTalkService>();

            // 检查是否启用了钉钉通知
            var dingTalkEnabled = _configuration.GetValue<bool>("DingTalkConfig:Enabled", true);
            if (!dingTalkEnabled)
            {
                _logger.LogInformation("钉钉通知已禁用，跳过发送统计数据");
                return;
            }

            var webhookUrl = _configuration.GetSection("DingTalkConfig:WebhookUrl").Value;
            if (string.IsNullOrEmpty(webhookUrl))
            {
                _logger.LogWarning("未配置钉钉机器人Webhook URL");
                return;
            }

            try
            {
                // 获取今天的统计数据
                var todayStart = DateTime.Today;
                var todayEnd = DateTime.Now; // 使用当前时间作为结束时间

                // 获取总数据条数
                var totalStats = await statisticsService.GetTotalDataCountAsync(todayStart, todayEnd);

                // 获取各变量的数据条数
                var variableStats = await statisticsService.GetVariableCountsAsync(todayStart, todayEnd);

                // 获取消息模板
                var template = _configuration.GetSection("DingTalkConfig:MessageTemplate").Value ?? @"## InfluxDB 数据统计报告 ({{date}})

### 数据概览
- **总数据条数**: {{total_count}}
- **统计时间**: {{start_time}} 至 {{end_time}}

### 变量数据分布
{{variable_stats}}";

                // 替换模板中的变量
                var markdownContent = template
                    .Replace("{{date}}", todayStart.ToString("yyyy-MM-dd"))
                    .Replace("{{total_count}}", (totalStats?.TotalCount ?? 0).ToString())
                    .Replace("{{start_time}}", $"{todayStart:yyyy-MM-dd} 00:00:00")
                    .Replace("{{end_time}}", $"{todayEnd:yyyy-MM-dd HH:mm:ss}")
                    .Replace("{{variable_stats}}", BuildVariableStatsMarkdown(variableStats));

                // 获取钉钉机器人密钥
                var secret = _configuration.GetSection("DingTalkConfig:Secret").Value ?? string.Empty;

                // 发送消息到钉钉
                var success = await dingTalkService.SendMarkdownMessageAsync(webhookUrl, secret, "InfluxDB 数据统计报告", markdownContent);

                if (success)
                {
                    _logger.LogInformation("成功发送每日统计数据到钉钉");
                }
                else
                {
                    _logger.LogError("发送每日统计数据到钉钉失败");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取或发送统计数据时发生错误");
            }
        }

        private string BuildVariableStatsMarkdown(IEnumerable<dynamic> variableStats)
        {
            if (variableStats != null && variableStats.Any())
            {
                var markdown = "| 变量名称 | 数据条数 |\n";
                markdown += "|--------|--------|\n";
                foreach (var stat in variableStats)
                {
                    // 根据实际对象类型进行访问
                    string variableName = stat.VariableName?.ToString() ?? stat.variableName?.ToString() ?? stat.name?.ToString() ?? "未知";
                    int count = Convert.ToInt32(stat.Count ?? stat.count ?? 0);
                    markdown += $"| {variableName} | {count} |\n";
                }
                return markdown;
            }
            else
            {
                return "暂无变量数据\n";
            }
        }
    }
}