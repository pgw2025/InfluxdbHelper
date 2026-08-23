using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace InfluxdbHelper.Api.Infrastructure
{
    /// <summary>
    /// 全局异常过滤器：把未捕获异常转换为统一 JSON 响应，替代旧版 catch 后静默返回 Page() 的模式。
    /// </summary>
    public class GlobalExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<GlobalExceptionFilter> _logger;

        public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
        {
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            _logger.LogError(context.Exception, "API 未处理异常: {Path}", context.HttpContext.Request.Path);

            var message = context.Exception.InnerException?.Message ?? context.Exception.Message;
            context.Result = new ObjectResult(ApiResponse.Fail(5000, $"服务器内部错误: {message}"))
            {
                StatusCode = StatusCodes.Status200OK // 业务码约定：HTTP 200 + Code 字段区分成败
            };
            context.ExceptionHandled = true;
        }
    }
}
