using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Models;
using Newtonsoft.Json.Linq;
using RaceManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            JObject result = new JObject();
            (List<EnteredEvent> waiting, List<(EnteredEvent id, TimingEvent timer)> onTrack, List<LeftEvent> unmatchedIds, List<TimingEvent> unmatchedTimes) = manager.GetState;


            result["waiting"] = JArray.FromObject(waiting);
            result["onTrack"] = JArray.FromObject(onTrack);
            result["unmatchedEndIds"] = JArray.FromObject(unmatchedIds);
            result["unmatchedEndTimes"] = JArray.FromObject(unmatchedTimes);

            return new JsonResult(result);
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
    }
}
