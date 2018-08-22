
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc.Formatters;
using System.Buffers;
using Swashbuckle.AspNetCore.Swagger;

namespace Neo.Notifications
{


    public class OutputFormatActionFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            var actionResult = context.Result as ObjectResult;
            if (actionResult == null) return;

            var paramObj = context.HttpContext.Request.Query["prettify"];
            var isPrettify = string.IsNullOrEmpty(paramObj) || bool.Parse(paramObj);

            if (!isPrettify) return;

            var settings = new Newtonsoft.Json.JsonSerializerSettings { Formatting = Formatting.Indented };

            actionResult.Formatters.Add(new JsonOutputFormatter(settings, ArrayPool<char>.Shared));
        }
    }

    public class NotificationStartup
    {

        public NotificationStartup(IConfiguration configuration)
        {
            Configuration = configuration;
            
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            
            services.AddMvc(options =>
            {
                options.Filters.Add(new OutputFormatActionFilter());
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Swashbuckle.AspNetCore.Swagger.Info { Title = "NEO Notification API", Version = "v1" });
            });

            services.AddCors();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "NEO Notification API v1");
                c.RoutePrefix = string.Empty;
            });

            app.UseMvcWithDefaultRoute();

            app.UseCors(options =>
            {
                options.AllowAnyOrigin();
                options.AllowAnyHeader();
                options.AllowAnyMethod();
            });
        }
    }

    public class NotificationApiApplication
    {

        public NotificationApiApplication()
        {

            BuildWebHost().RunAsync();
        }

        public static IWebHost BuildWebHost()
        {

            return WebHost.CreateDefaultBuilder()
                .ConfigureLogging( logging => logging.SetMinimumLevel(LogLevel.Error))
                .UseUrls(Settings.Default.REST.Hosts)
                .UseStartup<NotificationStartup>()
                .Build();
        }
    }
}
