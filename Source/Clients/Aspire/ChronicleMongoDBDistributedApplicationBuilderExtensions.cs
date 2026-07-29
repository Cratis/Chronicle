// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aspire.Hosting.ApplicationModel;
using Cratis.Chronicle.Aspire;

namespace Aspire.Hosting;

/// <summary>
/// Extension methods for adding a Chronicle-compatible MongoDB resource to an Aspire distributed application.
/// </summary>
public static class ChronicleMongoDBDistributedApplicationBuilderExtensions
{
    /// <summary>
    /// The port <c>mongod</c> listens on inside the container.
    /// </summary>
    const int MongoDBPort = 27017;

    /// <summary>
    /// The script that initiates the single-node replica set, tolerating an already initiated one.
    /// </summary>
    static readonly string _initiateReplicaSetScript =
        $"try {{ rs.status() }} catch (error) {{ rs.initiate({{ _id: \"{ChronicleContainerImageTags.MongoDBReplicaSetName}\", members: [{{ _id: 0, host: \"localhost:{MongoDBPort}\" }}] }}) }}";

    /// <summary>
    /// The container command that starts <c>mongod</c> as a replica set and initiates it once it accepts connections.
    /// </summary>
    /// <remarks>
    /// The initiation runs in a background subshell that first polls until <c>mongod</c> answers a ping, because
    /// <c>rs.initiate</c> fails against a server that has not finished starting. <c>exec docker-entrypoint.sh</c>
    /// keeps <c>mongod</c> as PID 1 so it still receives container signals and drops privileges the way the
    /// official image intends.
    /// </remarks>
    static readonly string _replicaSetEntrypointCommand =
        "( until mongosh --quiet --eval 'db.adminCommand({ ping: 1 })' >/dev/null 2>&1; do sleep 0.3; done; " +
        $"mongosh --quiet --eval '{_initiateReplicaSetScript}' ) & " +
        $"exec docker-entrypoint.sh mongod --replSet {ChronicleContainerImageTags.MongoDBReplicaSetName} --bind_ip_all";

    /// <summary>
    /// Adds a MongoDB resource configured the way Chronicle needs it — a self-initiating single-node replica set
    /// exposed through a connection string with <c>directConnection=true</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Chronicle relies on MongoDB transactions and change streams, which a standalone <c>mongod</c> does not
    /// support. Aspire's <c>AddMongoDB</c> starts exactly such a standalone server, so pointing Chronicle at it
    /// leaves observers, projections, and observable queries silently doing nothing. This method starts the
    /// official MongoDB image as a single-node replica set that initiates itself on first run, which is what
    /// Chronicle needs for local development and testing.
    /// </para>
    /// <para>
    /// The returned connection string carries <c>?directConnection=true</c>, so the driver talks to the
    /// host-mapped port directly instead of following the replica-set member host advertised by the server —
    /// that host (<c>localhost</c> inside the container) is not reachable from outside it, and following it
    /// makes the driver hang.
    /// </para>
    /// <para>
    /// Two resources are added: a container named <c>{name}-server</c> running MongoDB, and a connection-string
    /// resource named <paramref name="name"/> that the returned builder represents. Pass the returned builder
    /// straight to <see cref="Cratis.Chronicle.Aspire.ChronicleAspireBuilderExtensions.WithMongoDB"/>.
    /// </para>
    /// </remarks>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/> to add the resources to.</param>
    /// <param name="name">The name for the connection-string resource. Defaults to <c>"mongodb"</c>.</param>
    /// <param name="imageTag">
    /// Optional tag for the MongoDB container image. Defaults to
    /// <see cref="ChronicleContainerImageTags.MongoDBTag"/>.
    /// </param>
    /// <returns>An <see cref="IResourceBuilder{T}"/> for the MongoDB connection string.</returns>
    /// <example>
    /// <code>
    /// var mongo = builder.AddCratisChronicleMongoDB();
    /// builder.AddCratisChronicle("chronicle", chronicle => chronicle.WithMongoDB(mongo));
    /// </code>
    /// </example>
    public static IResourceBuilder<IResourceWithConnectionString> AddCratisChronicleMongoDB(
        this IDistributedApplicationBuilder builder,
        string name = "mongodb",
        string? imageTag = default)
    {
        var server = builder
            .AddContainer($"{name}-server", ChronicleContainerImageTags.MongoDBImage, imageTag ?? ChronicleContainerImageTags.MongoDBTag)
            .WithImageRegistry(ChronicleContainerImageTags.Registry)
            .WithEndpoint(targetPort: MongoDBPort, name: ChronicleContainerImageTags.MongoDBEndpointName)
            .WithEntrypoint("/bin/sh")
            .WithArgs("-c", _replicaSetEntrypointCommand);

        var endpoint = server.GetEndpoint(ChronicleContainerImageTags.MongoDBEndpointName);

        return builder.AddConnectionString(
            name,
            ReferenceExpression.Create($"mongodb://{endpoint.Property(EndpointProperty.Host)}:{endpoint.Property(EndpointProperty.Port)}/?directConnection=true"));
    }
}
