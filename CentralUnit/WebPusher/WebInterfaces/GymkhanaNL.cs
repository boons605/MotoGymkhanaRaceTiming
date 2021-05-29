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
                await http.GetAsync($"{baseUrl}/penalty?auth={authToken}&uniqueId={GetId(startId)}&dsq=1");
            }
            else if (lap.End is ManualDNFEvent || lap.End is UnitDNFEvent)
            {
                await http.GetAsync($"{baseUrl}/penalty?auth={authToken}&uniqueId={GetId(startId)}&dnf=1");
            }
            else
            {
                int penalty = lap.Penalties.Sum(p => p.Seconds);

                await http.GetAsync($"{baseUrl}/penalty?auth={authToken}&uniqueId={GetId(startId)}&time={penalty}");
            }
        }

        public async Task EndLap(IdEvent end)
        {
            await http.GetAsync($"{baseUrl}/end_ride?auth={authToken}&tag={end.Rider.Name}");
        }

        public async Task NewTime(Lap lap)
        {
            Guid startId = GetStartGuid(lap);
            onTrack.Remove(startId);

            if (lap.Disqualified)
            {
                await http.GetAsync($"{baseUrl}/penalty?auth={authToken}&uniqueId={GetId(startId)}$dsq=1");
                await http.GetAsync($"{baseUrl}/end_ride_with_result?auth={authToken}&tag={lap.End.Rider.Name}&uniqueId={GetId(startId)}");
            }
            else if (lap.End is ManualDNFEvent || lap.End is UnitDNFEvent)
            {
                HttpResponseMessage result = await http.GetAsync($"{baseUrl}/penalty?auth={authToken}&uniqueId={startId.GetHashCode()}&dnf=1");
                Console.WriteLine($"Gymkhana response (DNF): {result.StatusCode}, {await result.Content.ReadAsStringAsync()}");

                result = await http.GetAsync($"{baseUrl}/end_ride_with_result?auth={authToken}&tag={lap.End.Rider.Name}&uniqueId={GetId(startId)}");
                Console.WriteLine($"Gymkhana response (END): {result.StatusCode}, {await result.Content.ReadAsStringAsync()}");
            }
            else
            {
                HttpResponseMessage result = await http.GetAsync($"{baseUrl}/new_time?auth={authToken}&tag={lap.Rider.Name}&uniqueId={GetId(startId)}&time={lap.GetLapTime(false)}");
                Console.WriteLine($"Gymkhana response (NEW): {result.StatusCode}, {await result.Content.ReadAsStringAsync()}");

                int penalty = lap.Penalties.Sum(p => p.Seconds);

                if (penalty > 0)
                {
                    result = await http.GetAsync($"{baseUrl}/penalty?auth={authToken}&uniqueId={startId.GetHashCode()}&time={penalty}");
                    Console.WriteLine($"Gymkhana response (PENALTY): {result.StatusCode}, {await result.Content.ReadAsStringAsync()}");
                }
            }
        }

        public async Task StartLap(IdEvent start)
        {
            if(!onTrack.Contains(start.EventId))
            {
                HttpResponseMessage result = await http.GetAsync($"{baseUrl}/start_ride?auth={authToken}&tag={start.Rider.Name}&uniqueId={GetId(start.EventId)}");
                Console.WriteLine($"Gymkhana response (START): {result.StatusCode}, {await result.Content.ReadAsStringAsync()}");
            }
        }

        public async Task Clear()
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

        private int GetId(Guid g) => g.GetHashCode() < 0 ? -g.GetHashCode() : g.GetHashCode();
    }
}
