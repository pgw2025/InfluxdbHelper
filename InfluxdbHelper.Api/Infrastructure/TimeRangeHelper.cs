namespace InfluxdbHelper.Api.Infrastructure
{
    /// <summary>
    /// 时间段计算，与旧 Statistics/Index.cshtml.cs 的 CalculateTimeRange 逻辑一致。
    /// 支持周期：day(今日) / yesterday(昨日) / daybefore(前日) / week(本周) / month(本月) / custom(自定义)。
    /// </summary>
    public static class TimeRangeHelper
    {
        public static (DateTime start, DateTime end) Calculate(string? period, DateTime? customStart, DateTime? customEnd)
        {
            var now = DateTime.Now;

            if (period == "custom" && customStart.HasValue && customEnd.HasValue)
            {
                var s = customStart.Value;
                var e = customEnd.Value;
                if (s > e) (s, e) = (e, s);
                return (s, e);
            }

            return (period?.ToLowerInvariant()) switch
            {
                "yesterday" => (now.Date.AddDays(-1), now.Date.AddDays(-1).AddDays(1).AddTicks(-1)),
                "daybefore" => (now.Date.AddDays(-2), now.Date.AddDays(-2).AddDays(1).AddTicks(-1)),
                "week" => WeekRange(now),
                "month" => (new DateTime(now.Year, now.Month, 1), now),
                _ => (now.Date, now) // 默认 day
            };
        }

        private static (DateTime start, DateTime end) WeekRange(DateTime now)
        {
            var daysFromMonday = (int)now.DayOfWeek - 1;
            if (daysFromMonday < 0) daysFromMonday = 6; // 周日归入本周
            return (now.Date.AddDays(-daysFromMonday), now);
        }
    }
}
