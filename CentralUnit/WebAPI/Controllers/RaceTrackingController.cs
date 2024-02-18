using log4net;
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
using System.Linq;
using System.Text;

namespace WebAPI.Controllers
{
    /// <summary>
    /// This controller exposes all the endpoints necessary during an active race
    /// </summary>
    [ApiController]
    public class RaceTrackingController : ControllerBase
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private readonly RaceManager manager;

        /// <summary>
        /// Standard constructor
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="tracker"></param>
        public RaceTrackingController(ILogger<RaceTrackingController> logger, RaceManager tracker)
        {
            this.manager = tracker;
        }

        /// <summary>
        /// Returns the state of the current race. This includes riders waiting to start, riders on track and unmatches events for ending laps
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("[controller]/State")]
        public ActionResult GetState()
        {
            return WrapWithManagerCheck(() =>
            {
                JObject result = new JObject();
                (RiderReadyEvent waiting, List<(RiderReadyEvent rider, TimingEvent timer)> onTrack, List<TimingEvent> unmatchedTimes) = manager.GetState;


                if(waiting is null)
                {
                    result["waiting"] = null;
                }
                else
                {
                    result["waiting"] = JObject.FromObject(waiting);
                }

                result["onTrack"] = JArray.FromObject(onTrack);
                result["unmatchedEndTimes"] = JArray.FromObject(unmatchedTimes);

                return new JsonResult(result);
            });
        }

        /// <summary>
        /// Returns all timing events from the end gate available to be macthed to a start timing event.
        /// Ids can be used in <see cref="MatchEndTime(Guid, Guid)"/> to complete a lap
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("[controller]/PendingTimes")]
        public ActionResult PendingTimes()
        {
            return WrapWithManagerCheck(() =>
            {
                List<TimingEvent> times = manager.GetState.unmatchedTimes;

                return new JsonResult(times);
            });
        }

        /// <summary>
        /// Removes an end timing event from the system. Useful when something triggers the timing gate accidentally
        /// </summary>
        /// <param name="eventId"></param>
        /// <returns></returns>
        [HttpDelete]
        [Route("[controller]/PendingTime")]
        public ActionResult DeletePendingTime([FromQuery] Guid eventId)
        {
            return WrapWithManagerCheck(() =>
            {
                DeleteTimeEventArgs args = new DeleteTimeEventArgs(DateTime.Now, "staff", eventId);

                manager.AddEvent(args);

                return Ok();
            });
        }

        /// <summary>
        /// Returns all driven laps since the provided start
        /// </summary>
        /// <param name="start"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("[controller]/Laps")]
        public ActionResult GetLaps(int start = 0)
        {
            return WrapWithManagerCheck(() =>
            {
                return new JsonResult(manager.GetLapTimes(start));
            });
        }

        /// <summary>
        /// Returns the laps grouped by rider and sorted from fast to slow
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("[controller]/BestLapsByRider")]
        public ActionResult GetLapsByRider()
        {
            return WrapWithManagerCheck(() =>
            {
                return new JsonResult(manager.GetBestLaps());
            });
        }

        /// <summary>
        /// Returns all penalties that apply to the riders that are currently on track
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("[controller]/Penalties")]
        public ActionResult GetPendingPenalties()
        {
            return WrapWithManagerCheck(() =>
            {
                return new JsonResult(manager.GetPendingPenalties());
            });
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
        /// Starts the race manager from a file containing all events that happened during a race and replays those events
        /// </summary>
        /// <param name="summary"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("[controller]/Replay")]
        public StatusCodeResult Replay([FromBody] JObject summary)
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
        /// Starts the race manager from a file containing timing gate events and replays those events at the given time
        /// </summary>
        /// <param name="data"></param>
        /// <param name="delayMilliseconds"></param>
        /// <param name="overrideEventDelayMilliseconds"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("[controller]/Simulate")]
        public StatusCodeResult Simulate([FromBody] JObject data, [FromQuery] int delayMilliseconds, [FromQuery] int? overrideEventDelayMilliseconds)
        {
            manager.Start(data.ToObject<SimulationData>(), delayMilliseconds, overrideEventDelayMilliseconds);
            return new StatusCodeResult(200);
        }

        /// <summary>
        /// Returns all riders that have been added to the system and can start a lap
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("[controller]/GetRiders")]
        public ActionResult GetRiders()
        {
            return WrapWithManagerCheck(() =>
            {
                return new JsonResult(JArray.FromObject(manager.GetKnownRiders()));
            });

        }

        /// <summary>
        /// Adds a new rider to the running race
        /// </summary>
        /// <param name="rider"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("[controller]/Rider")]
        public ActionResult AddRider([FromBody] Rider rider)
        {
            return WrapWithManagerCheck(() => 
            {
                Log.Info($"addin rider {rider.Name}, {rider.Id}");
                manager.AddRider(rider); 
                return new StatusCodeResult(200); 
            });
        }

        /// <summary>
        /// Removes a rider from the running race
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete]
        [Route("[controller]/Rider")]
        public ActionResult DeleteRider([FromQuery] Guid id)
        {
            return WrapWithManagerCheck(() =>
            {
                manager.RemoveRider(id);
                return new StatusCodeResult(200);
            });
        }

        /// <summary>
        /// Changes the starting position of the rider with the given id
        /// </summary>
        /// <param name="riderId"></param>
        /// <param name="newPosition"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("[controller]/ChangeStartingOrder")]
        public ActionResult ChangeStartingOrder([FromQuery] Guid riderId, [FromQuery] int newPosition)
        {
            return WrapWithManagerCheck(() =>
            {
                try
                {
                    manager.ChangePosition(riderId, newPosition);
                    return new StatusCodeResult(200);
                }
                catch (Exception e) when (e is ArgumentException || e is KeyNotFoundException)
                {
                    JObject errorBody = new JObject
                    {
                        { "Error", $"Could not change rider starting order: {e.Message}" }
                    };

                    JsonResult errorResult = new JsonResult(errorBody);
                    errorResult.StatusCode = 400;

                    return errorResult;
                }
            });
        }

        /// <summary>
        /// Notifies the race manager that a new rider is waiting in the start box
        /// </summary>
        /// <param name="riderId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("[controller]/RiderReady")]
        public ActionResult RiderReader([FromQuery] Guid riderId)
        {
            return WrapWithManagerCheck(() =>
            {
                Log.Info($"/RiderReady for {riderId}");
                (RiderReadyEvent waiting, List<(RiderReadyEvent rider, TimingEvent timer)> onTrack, List<TimingEvent> unmatchedTimes) state = manager.GetState;

                JObject errorBody;

                // if the start box is empty
                if (state.waiting is null)
                {
                    // if the given rider is not on the track
                    if (!state.onTrack.Any(tuple => tuple.rider.Rider.Id == riderId))
                    {
                        Rider rider = manager.GetRiderById(riderId);

                        Log.Info($"Found rider on track {rider.Name}");

                        // if the given id corresponds to a known rider
                        if (rider is not null)
                        {
                            manager.AddEvent(new RiderReadyEventArgs(DateTime.Now, riderId, "staff"));

                            return new StatusCodeResult(200);
                        }
                        else
                        {
                            errorBody = new JObject
                            {
                                { "Error", $"The given rider id {riderId} does not correspond to a known rider" }
                            };
                        }
                    }
                    else
                    {
                        errorBody = new JObject
                        {
                            { "Error", $"The given rider id {riderId} is already on track" }
                        };
                    }
                }
                else
                {
                    errorBody = new JObject
                    {
                        { "Error", $"There is already a rider waiting: {state.waiting.Rider.Name}, id: {state.waiting.Rider.Id}" }
                    };
                }

                JsonResult errorResult = new JsonResult(errorBody);
                errorResult.StatusCode = 500;

                return errorResult;
            });

        }

        /// <summary>
        /// Notifies the race manager that the start box is empty.
        /// Can be used when a rider cannot start after entering the start box
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("[controller]/ClearStartBox")]
        public ActionResult ClearStartBox()
        {
            Log.Info($"/ClearStartBox");
            return WrapWithManagerCheck(() =>
            {
                manager.AddEvent(new ClearReadyEventArgs(DateTime.Now, Guid.Empty, "staff"));
                return new StatusCodeResult(200);
            });
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
        public ActionResult MatchEndTime([FromQuery] Guid riderId, [FromQuery] Guid timeId)
        {
            return WrapWithManagerCheck(() =>
            {
                Rider rider = manager.GetRiderById(riderId);

                if (rider is not null)
                {
                    manager.AddEvent(new RiderFinishedEventArgs(DateTime.Now, riderId, "staff", timeId));
                    return new StatusCodeResult(200);
                }
                else
                {
                    JObject errorBody = new JObject
                {
                    { "Error", $"The given rider id {riderId} does not correspond to a known rider" }
                };

                    JsonResult errorResult = new JsonResult(errorBody);
                    errorResult.StatusCode = 500;

                    return errorResult;
                }
            });
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

        /// <summary>
        /// Wraps endpoint code with a check and an approriate error for a running race manager
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        private ActionResult WrapWithManagerCheck(Func<ActionResult> endpoint)
        {
            if (manager.HasState)
            {
                return endpoint();
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
    }
}
