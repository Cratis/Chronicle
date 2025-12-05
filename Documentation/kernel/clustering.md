# Orleans Clustering Support

Chronicle Kernel uses Orleans clustering to allow running multiple instances of the Chronicle Server that work together. This provides high availability and scalability for your event store. Clustering is always enabled through MongoDB.

## Configuration

Clustering is configured through the `chronicle.json` configuration file or environment variables. Add the following to your configuration:

```json
{
    "clustering": {
        "clusterId": "chronicle-cluster",
        "serviceId": "chronicle"
    },
    "storage": {
        "type": "MongoDB",
        "connectionDetails": "mongodb://localhost:27017"
    }
}
```

### Configuration Options

- **clusterId**: A unique identifier for the cluster. All instances that should form a cluster must use the same `clusterId`.
- **serviceId**: A unique identifier for the service. All instances in the cluster must use the same `serviceId`.

### Environment Variables

You can also configure clustering using environment variables:

```bash
export Cratis__Chronicle__clustering__clusterId=chronicle-cluster
export Cratis__Chronicle__clustering__serviceId=chronicle
export Cratis__Chronicle__storage__connectionDetails=mongodb://localhost:27017
```

## Storage Requirements

Orleans clustering uses MongoDB as a shared storage backend for cluster membership information.

The MongoDB database specified in `storage.connectionDetails` stores:
- Cluster membership information
- Reminder data  
- Grain storage (if configured)

## Running Multiple Instances

To run multiple Chronicle Server instances in a cluster:

1. Ensure MongoDB is running and accessible to all instances
2. Configure all instances with the same `clusterId` and `serviceId`
3. Start each instance on different ports:

### Instance 1

```json
{
    "port": 35000,
    "apiPort": 8080,
    "clustering": {
        "clusterId": "chronicle-cluster",
        "serviceId": "chronicle"
    },
    "storage": {
        "type": "MongoDB",
        "connectionDetails": "mongodb://localhost:27017"
    }
}
```

### Instance 2

```json
{
    "port": 35001,
    "apiPort": 8081,
    "clustering": {
        "clusterId": "chronicle-cluster",
        "serviceId": "chronicle"
    },
    "storage": {
        "type": "MongoDB",
        "connectionDetails": "mongodb://localhost:27017"
    }
}
```

## Verifying the Cluster

You can verify that instances have joined the cluster by:

1. **Orleans Dashboard**: Each instance runs an Orleans Dashboard (default port 8081 for first instance, 8082 for second, etc.). Access it at `http://localhost:8081` to see cluster members.

2. **MongoDB**: Check the MongoDB database for cluster membership information:
   ```javascript
   use chronicle
   db.OrleansMembershipTable.find()
   ```

3. **Logs**: Check the server logs for messages indicating successful cluster formation and silo connectivity.

## Testing Clustering

To test clustering functionality:

1. Start a MongoDB instance:
   ```bash
   docker run -d -p 27017:27017 mongo
   ```

2. Create two configuration files (`chronicle1.json` and `chronicle2.json`) with different ports but the same cluster configuration.

3. Start two instances:
   ```bash
   # Terminal 1
   dotnet run --project Source/Kernel/Server -- --chronicle-config chronicle1.json
   
   # Terminal 2  
   dotnet run --project Source/Kernel/Server -- --chronicle-config chronicle2.json
   ```

4. Verify both instances appear in the Orleans Dashboard

5. Test failover by stopping one instance and verifying the other continues to function

## Production Considerations

When deploying Chronicle in a clustered configuration:

- **Load Balancing**: Use a load balancer to distribute requests across cluster members
- **Health Checks**: Configure health check endpoints for your load balancer (`/health` by default)
- **Network**: Ensure all cluster members can communicate with each other and MongoDB
- **Monitoring**: Monitor cluster health through Orleans Dashboard or custom metrics
- **Scaling**: You can add or remove cluster members dynamically - Orleans will automatically rebalance

## Troubleshooting

### Instances not forming a cluster

- Verify all instances use the same `clusterId` and `serviceId`
- Check MongoDB connectivity from all instances
- Ensure no firewall rules blocking Orleans ports
- Check Orleans Dashboard and logs for connection errors

### Split brain scenarios

- Orleans uses MongoDB for cluster membership to prevent split-brain
- Ensure MongoDB is highly available in production
- Monitor cluster membership in MongoDB to detect issues

## Limitations

- All instances must use the same MongoDB database for clustering
- Minimum of 2 instances recommended for high availability  
- Maximum cluster size depends on your infrastructure and workload

## See Also

- [Microsoft Orleans Clustering Documentation](https://learn.microsoft.com/en-us/dotnet/orleans/implementation/cluster-management)
- [Orleans.Providers.MongoDB](https://github.com/Orleans-Contrib/Orleans.Providers.MongoDB)
