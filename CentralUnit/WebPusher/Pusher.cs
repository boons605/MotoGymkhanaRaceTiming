using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using WebPusher.WebInterfaces;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json.Linq;
using Models;

namespace WebPusher
{
    /// <summary>
    /// This class polls the WebAPI project for the state of the race and sends updates to other webservices
    /// </summary>
    public class Pusher
    {
        private HttpClient http;
        private IWebInterface webService;
        private string baseUrl;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handler">The request handler, unless you're mocking just use <see cref="HttpClientHandler"/></param>
        /// <param name="webService">The web service to post updates to</param>
        /// <param name="baseUrl">The root url of the webAPI to get updates from like localhost:4000</param>
        public Pusher(HttpMessageHandler handler, IWebInterface webService, string baseUrl)
        {
            this.http = new HttpClient(handler);
            this.webService = webService;
            this.baseUrl = baseUrl;
        }

        public async Task Run(CancellationToken token)
        {
            int lapIndex = 0;

            while(!token.IsCancellationRequested)
            {
                HttpResponseMessage stateResponse = await http.GetAsync($"{baseUrl}/racetracking/State");

                string stateString = await stateResponse.Content.ReadAsStringAsync();

                JObject parsedContent = JObject.Parse(stateString);

                List<(IdEvent id, TimingEvent timer)> onTrack = parsedContent["onTrack"].ToObject<List<(IdEvent id, TimingEvent timer)>>();

                foreach((var id, _) in onTrack)
                {
                    await webService.StartLap(id);
                }

                string lapsString = await (await http.GetAsync($"{baseUrl}/racetracking/Laps?start={lapIndex}")).Content.ReadAsStringAsync();
                JObject parsedLaps = JObject.Parse(lapsString);

                List<Lap> laps = parsedLaps.ToObject<List<Lap>>();

                foreach(Lap lap in laps)
                {
                    await webService.NewTime(lap);
                }

                // lets not DoS the WebAPI
                await Task.Delay(1000);
            }
        }
    }
}
