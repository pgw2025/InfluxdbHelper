namespace InfluxdbHelper.Api.Infrastructure
{
    /// <summary>
    /// 统一 API 响应包装。
    /// </summary>
    public class ApiResponse
    {
        public int Code { get; set; }
        public string Message { get; set; } = "ok";
        public object? Data { get; set; }

        public static ApiResponse Ok(object? data = null, string message = "ok")
            => new() { Code = 0, Message = message, Data = data };

        public static ApiResponse Fail(int code, string message)
            => new() { Code = code, Message = message, Data = null };
    }

    /// <summary>
    /// 带 Data 类型约束的响应包装。
    /// </summary>
    public class ApiResponse<T> : ApiResponse
    {
        public new T? Data { get; set; }

        public static ApiResponse<T> Ok(T data, string message = "ok")
            => new() { Code = 0, Message = message, Data = data };
    }
}
