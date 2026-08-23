namespace InfluxdbHelper.DTOs
{
    public class DataStatisticsDto
    {
        public int TotalCount { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Period { get; set; } = string.Empty; // day, week, month
    }

    public class VariableStatisticsDto
    {
        public string VariableName { get; set; } = string.Empty;
        public int Count { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Period { get; set; } = string.Empty; // day, week, month
    }

    public class VariableValueDto
    {
        public string VariableName { get; set; } = string.Empty;
        public object Value { get; set; } = new object();
        public DateTime Time { get; set; }
    }
}