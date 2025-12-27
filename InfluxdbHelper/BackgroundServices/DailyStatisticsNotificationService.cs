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

            // 每天上午9点执行
            var now = DateTime.Now;
            var nextRun = new DateTime(now.Year, now.Month, now.Day, 9, 0, 0); // 每天上午9点
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

                // 构建消息内容
                var markdownContent = $"## InfluxDB 数据统计报告 ({todayStart:yyyy-MM-dd})\n\n" +
                                    $"### 数据概览\n" +
                                    $"- **总数据条数**: {totalStats?.TotalCount ?? 0}\n" +
                                    $"- **统计时间**: {todayStart:yyyy-MM-dd} 00:00:00 至 {todayEnd:yyyy-MM-dd HH:mm:ss}\n\n" +
                                    $"### 变量数据分布\n";

                if (variableStats != null && variableStats.Any())
                {
                    markdownContent += "| 变量名称 | 数据条数 |\n";
                    markdownContent += "|--------|--------|\n";
                    foreach (var stat in variableStats)
                    {
                        markdownContent += $"| {stat.VariableName} | {stat.Count} |\n";
                    }
                }
                else
                {
                    markdownContent += "暂无变量数据\n";
                }

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
    }
}