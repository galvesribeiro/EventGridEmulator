using EventGridEmulator.Grains;
using EventGridEmulator.Messages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EventGridEmulator.Controllers
{
    [Route("Microsoft.EventGrid")]
    [ApiController]
    public class SubscriptionsController : ControllerBase
    {
        private readonly IClusterClient _clusterClient;
        private readonly ILogger _logger;

        public SubscriptionsController(ILoggerFactory loggerFactory, IClusterClient clusterClient)
        {
            this._logger = loggerFactory.CreateLogger<SubscriptionsController>();
            this._clusterClient = clusterClient;
        }

        [HttpGet("eventSubscriptions")]
        public async Task<ActionResult<IReadOnlyDictionary<string, SubscriptionProperties>>> GetSubscriptions()
        {
            var subscriptionManager = this._clusterClient.GetGrain<ISubscriptionManagerGrain>(Guid.Empty);
            return Ok((await subscriptionManager.GetSubscriptions()).Value);
        }

        [HttpPost("eventSubscriptions/{eventSubscriptionName}")]
        public async Task<ActionResult> CreateSubscription(
            [FromRoute] string eventSubscriptionName,
            [FromBody] CreateSubscriptionRequest payload)
        {
            if (!this.ModelState.IsValid) return BadRequest("Invalid payload.");

            try
            {
                var subscriptionManager = this._clusterClient.GetGrain<ISubscriptionManagerGrain>(Guid.Empty);
                await subscriptionManager.CreateSubscription(eventSubscriptionName, payload);

                return Ok();
            }
            catch (Exception exc)
            {
                this._logger.LogError(exc, $"Failure to create subscription '{eventSubscriptionName}'.");
                return BadRequest(exc.Message);
            }
        }

        [HttpPost("events")]
        public async Task<ActionResult> DispatchEvents([FromBody] EventGridEvent[] events)
        {
            if (!this.ModelState.IsValid) return BadRequest("Invalid payload.");

            try
            {
                var subscriptionManager = this._clusterClient.GetGrain<ISubscriptionManagerGrain>(Guid.Empty);
                await subscriptionManager.Dispatch(events.AsImmutable());
                return Accepted();
            }
            catch (Exception exc)
            {
                this._logger.LogError(exc, $"Unable to dispatch events.");
                return BadRequest(exc.Message);
            }
        }
    }
}
