using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WebPusher.WebInterfaces
{
    public class GymkhanaNL : IWebInterface
    {
        private HashSet<Guid> onTrack = new HashSet<Guid>();
        private HttpClient http;
        private string authToken;
        private string baseUrl;

        /// <summary>
        /// This class provides an interface to the endpoints at https://timing.motogymkhana.nl
        /// Also keeps some internal state to avoid doing duplicate/unnecessary requests
        /// </summary>
        /// <param name="http">The http client to poll with, if you're mocking pass one with a cutom <see cref="HttpClientHandler"/></param>
        /// <param name="authToken">Authorization token necessary to access the endpoints</param>
        /// <param name="baseUrl">The url for the score keeping part of the dutch gykhana site</param>
        public GymkhanaNL(HttpClient http, string authToken, string baseUrl = "https://timing.motogymkhana.nl")
        {
            this.http = http;
            this.authToken = authToken;
            this.baseUrl = baseUrl;
        }

        public async Task AddPenalty(Lap lap)
        {
            Guid startId = GetStartGuid(lap);
            onTrack.Remove(startId);

            if (lap.Disqualified)
            {
                await http.GetAsync($"{baseUrl}/penalty?auth={authToken}&uniqueId={startId.GetHashCode()}$dsq=1");
            }
            else if (lap.End is ManualDNFEvent || lap.End is UnitDNFEvent)
            {
                await http.GetAsync($"{baseUrl}/penalty?auth={authToken}&uniqueId={startId.GetHashCode()}$dnf=1");
            }
            else
            {
                int penalty = lap.Penalties.Sum(p => p.Seconds);

                await http.GetAsync($"{baseUrl}/penalty?auth={authToken}&uniqueId={startId.GetHashCode()}$time={penalty}");
            }
        }

        public async Task EndLap(IdEvent end)
        {
            await http.GetAsync($"{baseUrl}/end_ride?auth={authToken}&tag={end.Rider.Beacon.Identifier}");
        }

        public async Task NewTime(Lap lap)
        {
            Guid startId = GetStartGuid(lap);
            onTrack.Remove(startId);

            if (lap.Disqualified)
            {
                await http.GetAsync($"{baseUrl}/penalty?auth={authToken}&uniqueId={startId.GetHashCode()}$dsq=1");
                await http.GetAsync($"{baseUrl}/end_ride_with_result?auth={authToken}&tag={lap.End.Rider.Beacon.Identifier}&uniqueId={startId.GetHashCode()}");
            }
            else if (lap.End is ManualDNFEvent || lap.End is UnitDNFEvent)
            {
                await http.GetAsync($"{baseUrl}/penalty?auth={authToken}&uniqueId={startId.GetHashCode()}$dnf=1");
                await http.GetAsync($"{baseUrl}/end_ride_with_result?auth={authToken}&tag={lap.End.Rider.Beacon.Identifier}&uniqueId={startId.GetHashCode()}");
            }
            else
            {
                await http.GetAsync($"{baseUrl}/new_time?auth={authToken}&tag={lap.Rider.Beacon.Identifier}&uniqueId={startId.GetHashCode()}$time={lap.GetLapTime(false)}");

                int penalty = lap.Penalties.Sum(p => p.Seconds);

                await http.GetAsync($"{baseUrl}/penalty?auth={authToken}&uniqueId={startId.GetHashCode()}$time={penalty}");
            }
        }

        public async Task StartLap(IdEvent start)
        {
            if(!onTrack.Contains(start.EventId))
            {
                await http.GetAsync($"{baseUrl}/start_ride?auth={authToken}&tag={start.Rider.Beacon.Identifier}&uniqueId={start.EventId.GetHashCode()}");
            }
        }

        public async Task CLear()
        {
            await http.GetAsync($"{baseUrl}/delete_todays_results?auth={authToken}");
        }

        private Guid GetStartGuid(Lap lap)
        {
            switch (lap.End)
            {
                case FinishedEvent finish:
                    return finish.Entered.EventId;
                case ManualDNFEvent manualDnf:
                    return manualDnf.ThisRider.EventId;
                case UnitDNFEvent unitDnf:
                    return unitDnf.ThisRider.EventId;
                default:
                    throw new ArgumentException($"Unkown lap event type: {lap.End.GetType()}");
            }
        }
    }
}
