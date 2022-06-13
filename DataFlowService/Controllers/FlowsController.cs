using DataFlowService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataFlowService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FlowsController : ControllerBase
    {
        private readonly IFlowAggregationService flowService;
        private readonly ILogger<FlowsController> _logger;

        public FlowsController(ILogger<FlowsController> logger, IFlowAggregationService flowService)
        {
            _logger = logger;
            this.flowService = flowService;
        }

        [HttpGet]
        public Task<IEnumerable<Flow>> Get([FromQuery]int hour)
        {
            if (hour <= 0 || hour > 24) throw new HttpResponseException(400, $"Invalid hour value of {hour} It must be between 1-24");
            return flowService.GetFlowsByHourAsync(hour);
        }

        [HttpPost]
        public async Task Post(IEnumerable<Flow> flows)
        {
            if (!flows.Any()) throw new HttpResponseException(400, $"POST message body must contains at least one flow object");

            // aggregate flows first on the front end, this helps distribute processing
            // to multiple instances of this service and reduce work on the actual
            // service that holds the aggregated data. 
            Dictionary<string, Flow> aggMap = new Dictionary<string, Flow>();
            foreach (var flow in flows)
            {
                string key = string.Format("{0}-{1}-{2}-{3}", flow.Hour, flow.SrcApp, flow.DestApp, flow.VpcId);
                if (aggMap.TryGetValue(key, out Flow f))
                {
                    f.BytesRx += flow.BytesRx;
                    f.BytesTx += flow.BytesTx;
                }
                else
                {
                    aggMap[key] = flow;
                }
            }
            
            await flowService.AddFlowsAsync(aggMap.Values);      
        }
    }
}
