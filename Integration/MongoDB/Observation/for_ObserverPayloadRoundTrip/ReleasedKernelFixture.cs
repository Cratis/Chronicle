// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using MongoDB.Driver;

namespace Cratis.Chronicle.MongoDB.Integration.Observation.for_ObserverPayloadRoundTrip;

/// <summary>
/// Runs a released kernel against an isolated MongoDB without starting a new client against its old wire API.
/// </summary>
public sealed class ReleasedKernelFixture : IAsyncDisposable
{
    internal const string StorageTypeKey = "Cratis__Chronicle__Storage__Type";
    internal const string StorageConnectionKey = "Cratis__Chronicle__Storage__ConnectionDetails";

    static readonly TimeSpan _readinessTimeout = TimeSpan.FromMinutes(3);

    readonly INetwork _network = new NetworkBuilder().Build();
    readonly IContainer _mongoDB;
    readonly IContainer _kernel;
    readonly ReleasedObserverReadyWait _ready;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReleasedKernelFixture"/> class.
    /// </summary>
    public ReleasedKernelFixture()
    {
        // Share the suite's MongoDB image (and its CHRONICLE_MONGODB_IMAGE override) so this fixture cannot drift to a
        // version a newer host kernel refuses to run; the released kernel's MongoDB.Driver 3.12 supports MongoDB 8.2.
        _mongoDB = new ContainerBuilder(ChronicleInProcessFixture.ImageName)
            .WithEnvironment("MONGO_INITDB_ROOT_USERNAME", "chronicle-test")
            .WithEnvironment("MONGO_INITDB_ROOT_PASSWORD", "chronicle-test")

            // Reverts to the pre-8.0.5 behavior where glibc, not TCMalloc, owns rseq registration - the documented
            // fix for MongoDB's Linux kernel 6.19+ incompatibility (https://jira.mongodb.org/browse/SERVER-121912).
            .WithEnvironment("GLIBC_TUNABLES", "glibc.pthread.rseq=1")
            .WithNetwork(_network)
            .WithNetworkAliases("mongodb")
            .WithPortBinding(27017, assignRandomHostPort: true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("mongosh", "--quiet", "--eval", "db.adminCommand({ ping: 1 })"))
            .Build();
        _ready = new(() => Client);
        _kernel = new ContainerBuilder("cratis/chronicle:19.106.2-development-slim@sha256:3dcf0cf274200619809573faa16395195364568a1162d9c210e815b5ba5a9643")
            .WithNetwork(_network)
            .WithEnvironment(StorageTypeKey, "MongoDB")
            .WithEnvironment(StorageConnectionKey, "mongodb://chronicle-test:chronicle-test@mongodb:27017/?authSource=admin")

            // The released kernel predates the dedicated health port: it serves /health over HTTPS on 35000 only.
            // Every wait is bounded so a readiness mismatch fails the spec instead of hanging the test host.
            .WithCreateParameterModifier(ReserveKernelPorts)
            .WithPortBinding(35000, true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .AddCustomWaitStrategy(new HttpsHealthWait(35000), options => options.WithTimeout(_readinessTimeout))
                .AddCustomWaitStrategy(_ready, options => options.WithTimeout(_readinessTimeout)))
            .Build();
    }

    /// <summary>
    /// Gets a host-side client for this fixture's isolated database.
    /// </summary>
    public IMongoClient Client => new MongoClient(new MongoClientSettings
    {
        Server = new MongoServerAddress(_mongoDB.Hostname, _mongoDB.GetMappedPublicPort(27017)),
        Credential = MongoCredential.CreateCredential("admin", "chronicle-test", "chronicle-test")
    });

    /// <summary>
    /// Gets the persisted old-kernel observer selected by the readiness signal.
    /// </summary>
    /// <exception cref="InvalidOperationException">The old kernel is not ready.</exception>
    public (string Database, string Identifier, EventSequenceId Owner) Observer => _ready.Observer ?? throw new InvalidOperationException("Old kernel is not ready.");

    /// <summary>
    /// Starts the database and released kernel.
    /// </summary>
    /// <returns>The asynchronous operation.</returns>
    public async Task Start()
    {
        await _network.CreateAsync();
        await _mongoDB.StartAsync();
        await _kernel.StartAsync();
    }

    internal static void ReserveKernelPorts(CreateContainerParameters parameters)
    {
        // The released kernel connects to MongoDB before binding HTTPS; exclude its listeners from outbound allocation.
        var hostConfig = parameters.HostConfig ?? new HostConfig();
        var sysctls = hostConfig.Sysctls ?? new Dictionary<string, string>();
        sysctls["net.ipv4.ip_local_reserved_ports"] = "11111,30000,35000";
        hostConfig.Sysctls = sysctls;
        parameters.HostConfig = hostConfig;
    }

    /// <summary>
    /// Stops the old writer while the new provider adds shadow metadata.
    /// </summary>
    /// <returns>The asynchronous operation.</returns>
    public Task StopKernel() => _kernel.StopAsync();

    /// <summary>
    /// Restarts the old writer against the augmented documents.
    /// </summary>
    /// <returns>The asynchronous operation.</returns>
    public Task RestartKernel() => _kernel.StartAsync();

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _kernel.DisposeAsync();
        await _mongoDB.DisposeAsync();
        await _network.DisposeAsync();
    }
}
