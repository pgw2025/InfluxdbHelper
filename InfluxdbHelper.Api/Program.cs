using System.Text;
using InfluxdbHelper.Services;
using InfluxdbHelper.Api.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace InfluxdbHelper.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ===== 控制器 =====
            builder.Services.AddControllers(options =>
            {
                options.Filters.Add<GlobalExceptionFilter>();
            });

            // ===== 业务服务（来自 Core 类库，与旧 Razor Pages 项目共享） =====
            builder.Services.AddScoped<IInfluxDBService, InfluxdbHelper.Services.InfluxDBService>();
            builder.Services.AddScoped<IStatisticsService, InfluxdbHelper.Services.StatisticsService>();
            builder.Services.AddScoped<IDingTalkService, InfluxdbHelper.Services.DingTalkService>();
            builder.Services.AddHttpClient();
            builder.Services.AddMemoryCache();
            builder.Services.AddHostedService<InfluxdbHelper.BackgroundServices.DailyStatisticsNotificationService>();

            // ===== JWT 认证 =====
            var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("appsettings 中缺少 Jwt:Key 配置");
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidateAudience = true,
                        ValidAudience = builder.Configuration["Jwt:Audience"],
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromSeconds(30)
                    };
                });
            builder.Services.AddAuthorization();

            // ===== CORS（开发环境 Vite 直连；生产走 Nginx 同域反代无需跨域） =====
            var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? new[] { "http://localhost:5173" };
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("Web", policy => policy
                    .WithOrigins(corsOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod());
            });

            // ===== Swagger =====
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "InfluxdbHelper API",
                    Version = "v1",
                    Description = "InfluxDB 数据统计工具后端接口"
                });
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "输入登录接口返回的 JWT Token"
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            var app = builder.Build();

            if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors("Web");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            // 默认监听 0.0.0.0（所有网卡），便于局域网 / 容器内被访问；
            // 可用环境变量 ASPNETCORE_URLS 或命令行 --urls 覆盖。
            var listenUrls = builder.Configuration["Urls"]
                ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
            if (!string.IsNullOrWhiteSpace(listenUrls))
            {
                foreach (var url in listenUrls.Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    app.Urls.Add(url.Trim());
                }
            }
            else
            {
                app.Urls.Add("http://0.0.0.0:5100");
            }

            app.Run();
        }
    }
}
