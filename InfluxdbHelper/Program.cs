using InfluxdbHelper.Services;

namespace InfluxdbHelper
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddScoped<IInfluxDBService, InfluxDBService>();
            builder.Services.AddScoped<IStatisticsService, StatisticsService>();
            builder.Services.AddScoped<IDingTalkService, DingTalkService>();
            builder.Services.AddHttpClient();
            builder.Services.AddHostedService<BackgroundServices.DailyStatisticsNotificationService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
            }
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapRazorPages();

            app.Run();
        }
    }
}
