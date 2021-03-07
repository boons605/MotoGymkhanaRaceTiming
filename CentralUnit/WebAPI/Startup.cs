using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Models;
using RaceManagement;
using SensorUnits.RiderIdUnit;
using SensorUnits.TimingUnit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WebAPI
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
            services.AddControllers();

            RaceSummary race;
            using (Stream input = new FileStream("D:\\Summary.json", FileMode.Open))
                race = RaceSummary.ReadSummary(input);

            SimulationRiderIdUnit startId = new SimulationRiderIdUnit(true, race);
            SimulationRiderIdUnit endId = new SimulationRiderIdUnit(false, race);
            SimulationTimingUnit timing = new SimulationTimingUnit(race);

            startId.Initialize();
            endId.Initialize();
            timing.Initialize();

            RaceTracker tracker = new RaceTracker(timing, startId, endId, timing.StartId, timing.EndId);

            tracker.OnRiderFinished += (o, e) => Console.WriteLine($"Rider {e.Finish.Rider.Name} finished with a lap time of {e.Finish.LapTime} microseconds");
            tracker.OnRiderDNF += (o, e) => Console.WriteLine($"Rider {e.Dnf.Rider.Name} did not finish since {e.Dnf.OtherRider.Rider.Name} finshed before them");
            tracker.OnRiderWaiting += (o, e) => Console.WriteLine($"Rider {e.Rider.Rider.Name} can start");
            tracker.OnStartEmpty += (o, e) => Console.WriteLine("Start box is empty");

            CancellationTokenSource source = new CancellationTokenSource();

            var trackTask = tracker.Run(source.Token);

            var startTask = startId.Run(source.Token);
            var endTask = endId.Run(source.Token);
            var timeTask = timing.Run(source.Token);

            var unitsTask = Task.WhenAll(startTask, endTask, timeTask);

            services.AddSingleton<RaceTracker>(tracker);

            services.AddMvc().AddNewtonsoftJson();
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

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
