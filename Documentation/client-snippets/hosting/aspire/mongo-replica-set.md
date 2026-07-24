```csharp
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Cratis.Chronicle.Aspire;

public static class HostingAspireMongoReplicaSet
{
    public static IResourceBuilder<ChronicleResource> ConfigureAppHost(IDistributedApplicationBuilder builder)
    {
        // Chronicle uses MongoDB transactions and change streams, so the database must run as a
        // replica set. This command starts a single-node replica set that initializes itself on
        // first run, keeping mongod as PID 1 for correct signal handling and privilege drop.
        const string replicaSetCommand =
            "( until mongosh --quiet --eval 'db.adminCommand({ ping: 1 })' >/dev/null 2>&1; do sleep 0.3; done; " +
            "mongosh --quiet --eval 'try { rs.status() } catch (error) { rs.initiate({ _id: \"rs0\", members: [{ _id: 0, host: \"localhost:27017\" }] }) }' ) & " +
            "exec docker-entrypoint.sh mongod --replSet rs0 --bind_ip_all";

        var mongo = builder.AddContainer("mongo", "mongo", "8.0")
            .WithEndpoint(targetPort: 27017, name: "tcp")
            .WithEntrypoint("/bin/sh")
            .WithArgs("-c", replicaSetCommand);

        var mongoEndpoint = mongo.GetEndpoint("tcp");

        // directConnection=true stops the driver from following the advertised replica-set member
        // host (localhost:27017, only reachable inside the container) back out and hanging.
        var mongoConnection = builder.AddConnectionString(
            "chronicle-mongo",
            ReferenceExpression.Create(
                $"mongodb://{mongoEndpoint.Property(EndpointProperty.Host)}:{mongoEndpoint.Property(EndpointProperty.Port)}/?directConnection=true"));

        return builder.AddCratisChronicle("chronicle", c => c.WithMongoDB(mongoConnection));
    }
}
```
