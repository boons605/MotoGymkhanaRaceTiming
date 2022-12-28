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

        /// <summary>
        /// Returns the state of the current race. This includes riders waiting to start, riders on track and unmatches events for ending laps
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("[controller]/State")]
        public JsonResult GetState()
        {
            if (manager.HasState)
            {
                JObject result = new JObject();
                (RiderReadyEvent waiting, List<(RiderReadyEvent rider, TimingEvent timer)> onTrack, List<TimingEvent> unmatchedTimes) = manager.GetState;


                result["waiting"] = JObject.FromObject(waiting);
                result["onTrack"] = JArray.FromObject(onTrack);
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

        /// <summary>
        /// Returns all timing events from the end gate available to be macthed to a start timing event.
        /// Ids can be used in <see cref="MatchEndTime(Guid, Guid)"/> to complete a lap
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("[controller]/PendingTimes")]
        public JsonResult PendingTimes()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns all driven laps since the provided start
        /// </summary>
        /// <param name="start"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Returns the laps grouped by rider and sorted from fast to slow
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Starts the race manager with the specified configuration
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("[controller]/Config")]
        public StatusCodeResult SetConfiguration([FromBody] RaceConfig config)
        {
            manager.Start(config, new List<Rider>());

            return new StatusCodeResult(200);
        }

        /// <summary>
        /// Starts the race manager to replay the given events
        /// </summary>
        /// <param name="summary"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Adds a new rider to the running race
        /// </summary>
        /// <param name="rider"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("[controller]/Rider")]
        public StatusCodeResult AddRider([FromBody] Rider rider)
        {
            manager.AddRider(rider);
            return new StatusCodeResult(200);
        }

        /// <summary>
        /// Removes a rider from the running race
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [HttpDelete]
        [Route("[controller]/Rider")]
        public StatusCodeResult DeleteRider([FromQuery] Guid id)
        {
            manager.RemoveRider(id);
            return new StatusCodeResult(200);
        }

        /// <summary>
        /// Notifies the race manager that a new rider is waiting in the start box
        /// </summary>
        /// <param name="riderID"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("[controller]/RiderReady")]
        public StatusCodeResult RiderReader([FromQuery] Guid riderID)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Notifies the race manager that the start box is empty.
        /// Can be used when a rider cannot start after entering the start box
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("[controller]/ClearStartBox")]
        public StatusCodeResult ClearStartBox()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Ends a lap by assigning a timing event from the end gat to a rider currently on track.
        /// If called with a rider that is not currently on track the timing event will remain available for matching
        /// </summary>
        /// <param name="riderId"></param>
        /// <param name="timeId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("[controller]/MatchEndTime")]
        public JsonResult MatchEndTime([FromQuery] Guid riderId, [FromQuery] Guid timeId)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Adds a penalty to the most recently driven lap (or lap being driven) of a rider
        /// </summary>
        /// <param name="penalty"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("[controller]/Penalty")]
        public StatusCodeResult AddPenalty([FromBody] PenaltyEventArgs penalty)
        {
            manager.AddEvent(new PenaltyEventArgs(DateTime.Now, penalty.RiderId, penalty.StaffName, penalty.Reason, penalty.Seconds));
            return new StatusCodeResult(200);
        }

        /// <summary>
        /// Ends the lap of a rider currently on track without a lap time.
        /// Can be used when the rider leaves the track without going through the end timing gate
        /// </summary>
        /// <param name="dnf"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("[controller]/DNF")]
        public StatusCodeResult AddDNF([FromBody] ManualDNFEventArgs dnf)
        {
            manager.AddEvent(new ManualDNFEventArgs(DateTime.Now, dnf.RiderId, dnf.StaffName));
            return new StatusCodeResult(200);
        }

        /// <summary>
        /// Adds a disqualified penalty to the most recently driven lap (or lap being driven) by a rider.
        /// This does not end the lap, the rider can still finish the lap normally
        /// </summary>
        /// <param name="dsq"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("[controller]/DSQ")]
        public StatusCodeResult AddDSQ([FromBody] DSQEventArgs dsq)
        {
            manager.AddEvent(new DSQEventArgs(DateTime.Now, dsq.RiderId, dsq.StaffName, dsq.Reason));
            return new StatusCodeResult(200);
        }
    }
}
