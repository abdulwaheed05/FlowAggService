using DataFlowService.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataFlowService
{
    /// <summary>
    /// Interface for flow aggregation service. Any backend service could implement this
    /// and could be integrated with our web api
    /// </summary>
    public interface IFlowAggregationService
    {
        Task<IEnumerable<Flow>> GetFlowsByHourAsync(int hour);
        Task AddFlowAsync(Flow flow);
        Task AddFlowsAsync(IEnumerable<Flow> flow);
    }
}
