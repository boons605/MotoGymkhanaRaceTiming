using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using log4net;
using log4net.Repository.Hierarchy;
using log4net.Appender;
using Newtonsoft.Json.Linq;
using System.IO;

namespace WebAPI.Controllers
{
    /// <summary>
    /// This controller provides endpoints to manage the system. These endpoints are not required during a race
    /// </summary>
    [ApiController]
    public class ManagementController : ControllerBase
    {
        /// <summary>
        /// Returns the 20 most recent lines in the system log
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("[controller]/LogEntries")]
        public JsonResult GetLogEntries()
        {
            FileAppender logAppender = ((Hierarchy)LogManager.GetRepository())
                                         .Root.Appenders.OfType<FileAppender>()
                                         .FirstOrDefault();

            if(logAppender == null)
            {
                JObject errorBody = new JObject
                {
                    {"Error", "Could not find a log file appender, is the log config correct?" }
                };

                JsonResult errorResult = new JsonResult(errorBody);
                errorResult.StatusCode = 500;

                return errorResult;
            }

            logAppender.Flush(100);

            using (StreamReader logStream = new StreamReader(new FileStream(logAppender.File, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                List<string> lines = new List<string>();

                while(!logStream.EndOfStream)
                {
                    lines.Add(logStream.ReadLine());
                }

                // show only the most recent 20 entries
                JArray body = new JArray(lines.Skip(lines.Count - 20).ToList());

                return new JsonResult(body);
            }
        }
    }
}
