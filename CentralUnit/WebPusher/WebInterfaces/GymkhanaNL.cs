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
        /// <param name="handler">The request handler, unless you're mocking just use <see cref="HttpClientHandler"/></param>
        /// <param name="authToken">Authorization token necessary to access the endpoints</param>
        /// <param name="baseUrl">The url for the score keeping part of the dutch gykhana site</param>
        public GymkhanaNL(HttpMessageHandler handler, string authToken, string baseUrl = "https://timing.motogymkhana.nl")
        {
            http = new HttpClient(handler);
            this.authToken = authToken;
            this.baseUrl = baseUrl;
        }

        public Task AddPenalty(Lap lap, PenaltyEvent penalty)
        {
            throw new NotImplementedException();
        }

        public Task AddPenalty(Lap lap, ManualDNFEvent dnf)
        {
            throw new NotImplementedException();
        }

        public Task AddPenalty(Lap lap, DSQEvent dsq)
        {
            throw new NotImplementedException();
        }

        public Task Clear()
        {
            throw new NotImplementedException();
        }

        public Task EndLap(IdEvent end)
        {
            throw new NotImplementedException();
        }

        public async Task NewTime(Lap lap)
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
