using DataFlowService.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataFlowService
{
    /// <summary>
    /// In memory flow aggregation service. internally keeps hour to flow mapping
    /// Ideally this would a separate service and an existing distributed solution like redis
    /// could be used.
    /// </summary>
    public class InMemoryFlowAggregationService : IFlowAggregationService
    {
        private ConcurrentDictionary<int, ConcurrentDictionary<string, Flow>> hourMap;
       
        public InMemoryFlowAggregationService()
        {
            hourMap = new ConcurrentDictionary<int, ConcurrentDictionary<string, Flow>>();
        }

        public Task<IEnumerable<Flow>> GetFlowsByHourAsync(int hour)
        {
            if(hourMap.TryGetValue(hour, out ConcurrentDictionary<string, Flow> value))
            {
                return Task.FromResult<IEnumerable<Flow>>(value.Values);
            }

            return Task.FromResult(Enumerable.Empty<Flow>());
        }

        public async Task AddFlowsAsync(IEnumerable<Flow> flows)
        {
            foreach (var flow in flows)
            {
                await AddFlowAsync(flow);
            }
        }

        public Task AddFlowAsync(Flow flow)
        {
            string key = string.Format("{0}-{1}-{2}-{3}", flow.Hour, flow.SrcApp, flow.DestApp, flow.VpcId);
            hourMap.AddOrUpdate(flow.Hour, (h) =>
            {          
                // Adds the new key-value (in this case dictionary)
                var map = new ConcurrentDictionary<string, Flow>();
                map[key] = flow;
            
                return map;
            }, (h, values) =>
            {
                // Updates the existing flow value by calling addorupdate.
                values.AddOrUpdate(key, flow, (k, v) =>
                {
                    v.BytesRx += flow.BytesRx;
                    v.BytesTx += flow.BytesTx;
            
                    return v;
                });
            
                return values;
            });

            return Task.CompletedTask;
        }
    }
}
