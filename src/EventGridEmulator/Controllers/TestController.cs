using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventGridEmulator.Messages;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace EventGridEmulator.Controllers
{
    [Route("test")]
    [ApiController]
    public class TestControllerController : ControllerBase
    {
        [HttpPost]
        public ActionResult<EndpointValidationResponse> Post([FromBody] EventGridEvent[] eventGridEvents)
        {
            const string SubscriptionValidationEvent = "Microsoft.EventGrid.SubscriptionValidationEvent";

            foreach (EventGridEvent eventGridEvent in eventGridEvents)
            {
                JObject dataObject = eventGridEvent.Data as JObject;

                // Deserialize the event data into the appropriate type based on event type
                if (string.Equals(eventGridEvent.EventType, SubscriptionValidationEvent, StringComparison.OrdinalIgnoreCase))
                {
                    var eventData = dataObject.ToObject<EndpointValidationData>();
                    //log.Info($"Got SubscriptionValidation event data, validation code: {eventData.ValidationCode}, topic: {eventGridEvent.Topic}");
                    // Do any additional validation (as required) and then return back the below response
                    var responseData = new EndpointValidationResponse
                    {
                        ValidationResponse = eventData.ValidationCode
                    };
                    return Ok(responseData);
                }
            }

            return BadRequest();
        }
    }
}
