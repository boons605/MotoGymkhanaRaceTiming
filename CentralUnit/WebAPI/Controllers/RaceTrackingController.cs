using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Models;
using Models.Config;
using Newtonsoft.Json.Linq;
using RaceManagement;
using System.Collections.Generic;

namespace WebAPI.Controllers
{
    [ApiController]
    public class RaceTrackingController : ControllerBase
    {
        private readonly ILogger<RaceTrackingController> logger;
        private readonly RaceManager manager;

        public RaceTrackingController(ILogger<RaceTrackingController> logger, RaceManager tracker)
        {
            this.logger = logger;
            this.manager = tracker;
        }

        [HttpGet]
        [Route("[controller]/State")]
        public JsonResult GetState()
        {
            //The combined task of running race management is only null before its first run
            if (manager.CombinedTasks != null)
            {
                JObject result = new JObject();
                (List<IdEvent> waiting, List<(IdEvent id, TimingEvent timer)> onTrack, List<IdEvent> unmatchedIds, List<TimingEvent> unmatchedTimes) = manager.GetState;


                result["waiting"] = JArray.FromObject(waiting);
                result["onTrack"] = JArray.FromObject(onTrack);
                result["unmatchedEndIds"] = JArray.FromObject(unmatchedIds);
                result["unmatchedEndTimes"] = JArray.FromObject(unmatchedTimes);

                return new JsonResult(result);
            }
            else
            {
                JObject body = new JObject
                {
                    { "Error", "Race tracking is not running. Provide a config first" }
                };

                JsonResult response = new JsonResult(body);
                response.StatusCode = 500;

                return response;
            }
        }

        [HttpGet]
        [Route("[controller]/Laps")]
        public JsonResult GetLaps(int start = 0)
        {
            return new JsonResult(JArray.FromObject(manager.GetLapTimes(start)));
        }

        [HttpGet]
        [Route("[controller]/BestLapsByRider")]
        public JsonResult GetLapsByRider()
        {
            return new JsonResult(JArray.FromObject(manager.GetBestLaps()));
        }

        [HttpPost]
        [Route("[controller]/Config")]
        public StatusCodeResult SetConfiguration([FromBody] RaceConfig config)
        {
            manager.Start(config, new List<Rider>());

            return new StatusCodeResult(200);
        }

        [HttpPost]
        [Route("[controller]/Rider")]
        public StatusCodeResult AddRider([FromBody] Rider rider)
        {
            manager.AddRider(rider);
            return new StatusCodeResult(200);
        }

        [HttpDelete]
        [Route("[controller]/Rider")]
        public StatusCodeResult DeleteRider([FromQuery] string name)
        {
            manager.RemoveRider(name);
            return new StatusCodeResult(200);
        }
    }
}
