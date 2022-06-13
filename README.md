# FlowAggService

Flow Aggregation Service (FAS)

Flow Aggregation service (FAS) enables users (other services) to write network flow data and read aggregation of that flow data by hour. Network flow is defined as an below
•	src_app - string
•	dest_app - string
•	vpc_id - string
•	bytes_tx - int
•	bytes_rx - int
•	hour - int 
The core functionality requirements of Flow aggregation service are
1. Accept network flow data points via a Write API
2. Aggregate those data points to accumulate the bytes transferred per flow
3. Serve the aggregated network flow data via a Read API

Design
This service could potentially have a large number of users (agents, possibly millions) that read/write network flow data, therefore the design of this service needs to take scalability into consideration. 
The design proposed here is to have two separate microservices.
1.	Data Flow service: This is a stateless, front-end service that can be scaled by adding more instances. It provides the APIs for read/write and does validation of the input data. Our requirements do not suggest storing the actual network flow records and therefore this service does an initial aggregation on the input data by combining network flows that have the same key (src, dest, vpc_id and hour) and then stores that aggregated data in a backend aggregation service which acts like a key-value store.  This initial aggregation improves write times by reducing the number of writes (updates) on aggregation service. It is also reduces the number of network calls to the key-value store service because we only make one call per unique network flow.

2.	Flow Aggregation Store: The is a stateful service that keeps a mapping of flow keys (src, dest, vpc_id, hour) to flow. We could choose to store the aggregation in database and read/write everytime but since our requirements are simple and there is no need to use a relational database we will use a in-memory key-value store. We can use something like a redis which provides fault tolerance and high availability by replicating data to multiple replicas. Only primary serves requests write requests and in case of failure a secondary can be promoted to primary.  
 
Implementation
The specific implementation for this service separates the two microservices into two different dynamic libraries to mimic the above design. 
The Data Flow service project uses AspNet core that provides the http APIs for reading/writing network flow data. One of the scalability limitations could be the number of network connections from users to this service and therefore adding more instances of this service could solve that problem. This service could easily scale to billions of calls per second.
The InMemoryFlowAggregationService provides an implementation of the IFlowAggregation service to store the actual aggregated data. ConcurrentDictionary is used to handle write access to the aggregated data by multiple threads. Since this service needs to write / or update existing data, it must always be consistent. To achieve consistency, we must only allow writing through the primary replica (instance) of this service. This could become a bottleneck for the scalability of our service. To improve scale of and throughout of this service we could choose to shard/partition this service. Because user queries are always for data that are per hour, one way to shard the data is by having a shard for each hour or group of hours.
Scalability Limits
Data Flow Service
This is a stateless service which is just receiving the network flows, does some basic aggregation and then calls the stateful service to store/update those aggregations. The main scalability limitations of this service are
1.	Number of concurrent open TCP network connections – Which I believe are 16 million on a windows server
2.	Amount of Server memory for concurrent connections (to hold the network flow objects, while they are being aggregated/saved into backend) – Assuming every flow object is 1KB, and a server with 32GB of memory, each server could hold ~33 million flow objects concurrently while they are being processed.
Both of these limitations are easily surpassed by adding more instances of this stateless service.
Flow Aggregation Store
This is the cache service we use to store the aggregated flow data in-memory for faster reads/writes. There are two limitations of this service. First, there because this is a stateful service, only primary replica can serve write request, so the number of concurrent requests are limited to the number of concurrent TCP connections on a single machine. Second, as we estimated above, if the size of each network flow object is ~1KB and memory of a server is ~32 GB then this service is limited to storing around ~33 million unique flows.
To surpass these limitations we have two options.
1.	For memory limitations we may choose to simple acquire servers with higher memory. These are easily available. If that is not sufficient, we can shard the data and have each shard store a range of hours for example we could choose shard A to store hours 1-4, that way we can store 6 x ~33 million unique network flows. The sharding strategy needs a bit more thought though, because if the nature of flows is not predictable then we may have some hot shards with too much traffic while other with not enough traffic.

2.	Sharding data will also help with concurrent TCP network connection limitation by spreading the load to different servers. 

Building and Running the Solution

This solution is developed using Visual Studio and in AspNet Core 3.1. To install these please have a look at this youtube video. https://www.youtube.com/watch?v=8ZWs_g2aR9g 
After installing visual studio, you can checkout the repo and open the DataFlowApplication.sln
Go to Build Menu and click “Build Solution” then goto Debug Menu and click “Start without Debugging”

