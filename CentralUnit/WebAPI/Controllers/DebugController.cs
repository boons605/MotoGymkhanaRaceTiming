using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Models;
using Newtonsoft.Json.Linq;
using RaceManagement;
using SensorUnits.TimingUnit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebAPI.Controllers
{
    [ApiController]
    public class DebugController : ControllerBase
    {
        private readonly ILogger<RaceTrackingController> logger;
        private readonly RaceManager manager;

        public DebugController(ILogger<RaceTrackingController> logger, RaceManager tracker)
        {
            this.logger = logger;
            this.manager = tracker;
        }

        [HttpPost]
        [Route("[controller]/TriggerTiming")]
        public ActionResult TriggerTiming([FromBody] TimingTriggeredEventArgs timingEvent)
        {
            manager.AddEvent(timingEvent);

            return new StatusCodeResult(200);
        }

        [HttpPost]
        [Route("[controller]/TriggerTimingGate")]
        public ActionResult TriggerTimingGate([FromForm] int gateId)
        {
            manager.TriggerTimingEvent(gateId);

            return new StatusCodeResult(200);
        }
    }
}
