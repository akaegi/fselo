using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FsElo.WebApp.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FsElo.WebApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.ProvideCosmosClient();
            services.ProvideEveneum();
            services.AddTransient<ScoreboardCommandHandler>();
            services.AddTransient<ScoreboardReadModelUpdater>();
            services.AddTransient<ScoreboardReadModelDataAccess>();
            services.AddMemoryCache(options =>
            {
                // size limit = number of UTF-8 chars = 1 MB.
                options.SizeLimit = 1 * 1024 * 1024;
            });
            
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}