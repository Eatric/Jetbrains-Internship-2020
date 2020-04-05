using System;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CIServerBlazor.Data;
using CIServerBlazor.Workers;
using LiteDB;

namespace CIServerBlazor
{
	public class Startup
	{
		public static ServiceProvider serviceProvider;

		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddRazorPages();
			services.AddServerSideBlazor();
			services.AddSingleton<BuildService>();
			serviceProvider = services.BuildServiceProvider();

			AddFirstWorker();
		}

		private void AddFirstWorker()
		{
			var gitUrl = Configuration.GetValue<string>("gitUrl").Substring(6);
			var branch = Configuration.GetValue<string>("branch");
			var buildCmd = Configuration.GetValue<string>("buildCmd");

			serviceProvider.GetService<BuildService>().AddWorker(gitUrl, branch, buildCmd);
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			else
			{
				app.UseExceptionHandler("/Error");
			}

			app.UseStaticFiles();

			app.UseRouting();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllerRoute("Workers", "/api/{controller=Workers}");
				endpoints.MapBlazorHub();
				endpoints.MapFallbackToPage("/_Host");
			});
		}
	}
}
