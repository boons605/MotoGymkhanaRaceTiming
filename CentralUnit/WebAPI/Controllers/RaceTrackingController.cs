using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Models;
using Models.Config;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RaceManagement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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
            if (manager.HasState)
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
        [Route("[controller]/Beacons")]
        public JsonResult GetBeacons()
        {
            if (manager.HasState)
            {
                JObject result = new JObject();
                (Beacon start, Beacon end) = manager.GetBeacons;

                if (start != null)
                {
                    result["start"] = JObject.FromObject(start);
                }

                if (end != null)
                {
                    result["end"] = JObject.FromObject(end);
                }

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
            if (manager.HasState)
            {
                return new JsonResult(manager.GetLapTimes(start), new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });
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
        [Route("[controller]/BestLapsByRider")]
        public JsonResult GetLapsByRider()
        {
            if (manager.HasState)
            {
                return new JsonResult(JArray.FromObject(manager.GetBestLaps()));
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

        [HttpPost]
        [Route("[controller]/Config")]
        public StatusCodeResult SetConfiguration([FromBody] RaceConfig config)
        {
            manager.Start(config, new List<Rider>());

            return new StatusCodeResult(200);
        }

        [HttpPost]
        [Route("[controller]/Simulate")]
        public StatusCodeResult Simulate([FromBody] JObject summary)
        {
            byte[] text = Encoding.UTF8.GetBytes(summary.ToString());
            RaceSummary parsed;
            using (MemoryStream stream = new MemoryStream(text))
            {
                parsed = RaceSummary.ReadSummary(stream);
            }
            manager.Start(parsed);
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

        [HttpPost]
        [Route("[controller]/Penalty")]
        public StatusCodeResult AddPenalty([FromBody] PenaltyEventArgs penalty)
        {
            manager.AddEvent(new PenaltyEventArgs(DateTime.Now, penalty.RiderName, penalty.StaffName, penalty.Reason, penalty.Seconds));
            return new StatusCodeResult(200);
        }

        [HttpPost]
        [Route("[controller]/DNF")]
        public StatusCodeResult AddDNF([FromBody] ManualDNFEventArgs dnf)
        {
            manager.AddEvent(new ManualDNFEventArgs(DateTime.Now, dnf.RiderName, dnf.StaffName));
            return new StatusCodeResult(200);
        }

        [HttpPost]
        [Route("[controller]/DSQ")]
        public StatusCodeResult AddDSQ([FromBody] DSQEventArgs dsq)
        {
            manager.AddEvent(new DSQEventArgs(DateTime.Now, dsq.RiderName, dsq.StaffName, dsq.Reason));
            return new StatusCodeResult(200);
        }
    }
}
