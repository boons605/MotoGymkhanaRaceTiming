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
    /// <summary>
    /// This controller provides endpoints that are convenient for testing the system. Do not use while running a race
    /// </summary>
    [ApiController]
    public class DebugController : ControllerBase
    {
        private readonly ILogger<RaceTrackingController> logger;
        private readonly RaceManager manager;

        /// <summary>
        /// Standard constructor
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="tracker"></param>
        public DebugController(ILogger<RaceTrackingController> logger, RaceManager tracker)
        {
            this.logger = logger;
            this.manager = tracker;
        }

        /// <summary>
        /// Send a custom timing event to the system
        /// </summary>
        /// <param name="timingEvent"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("[controller]/TriggerTiming")]
        public ActionResult TriggerTiming([FromBody] TimingTriggeredEventArgs timingEvent)
        {
            manager.AddEvent(timingEvent);

            return new StatusCodeResult(200);
        }

        /// <summary>
        /// Send a timing event to the system with a timestamp containing microseconds since the race has started
        /// </summary>
        /// <param name="gateId"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("[controller]/TriggerTimingGate")]
        public ActionResult TriggerTimingGate([FromForm] int gateId)
        {
            manager.TriggerTimingEvent(gateId);

            return new StatusCodeResult(200);
        }
    }
}
