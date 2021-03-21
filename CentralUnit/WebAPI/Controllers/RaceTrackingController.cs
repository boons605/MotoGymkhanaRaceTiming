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
    [Route("[controller]")]
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
        public JsonResult Get()
        {
            JObject result = new JObject();
            (List<EnteredEvent> waiting, List<(EnteredEvent id, TimingEvent timer)> onTrack, List<LeftEvent> unmatchedIds, List<TimingEvent> unmatchedTimes) = manager.GetState;


            result["waiting"] = JArray.FromObject(waiting);
            result["onTrack"] = JArray.FromObject(onTrack);
            result["unmatchedEndIds"] = JArray.FromObject(unmatchedIds);
            result["unmatchedEndTimes"] = JArray.FromObject(unmatchedTimes);

            return new JsonResult(result);
        }
    }
}
