// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.MongoDB.Integration.Observation.for_ObserverPayloadRoundTrip;
using Cratis.Chronicle.Storage.MongoDB.Generations;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.MongoDB.Integration.Generations.for_ReleasedKernelGenerationFloor;

/// <summary>
/// Runs one released kernel image against an isolated MongoDB, so a store's durable generation floor can be seeded between
/// kernel starts. Container logs are only searched, never printed.
/// </summary>
public sealed class ReleasedFloorKernel : IAsyncDisposable
{
    static readonly TimeSpan _readinessTimeout = TimeSpan.FromMinutes(3);
    static readonly TimeSpan _populateTimeout = TimeSpan.FromSeconds(120);
    static readonly TimeSpan _exitTimeout = TimeSpan.FromSeconds(120);

    readonly string _image;
    readonly INetwork _network = new NetworkBuilder().Build();
    readonly IContainer _mongoDB;
    readonly List<IContainer> _kernels = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="ReleasedFloorKernel"/> class.
    /// </summary>
    /// <param name="image">The released kernel image.</param>
    public ReleasedFloorKernel(string image)
    {
        _image = image;
        _mongoDB = new ContainerBuilder(ChronicleInProcessFixture.ImageName)
            .WithEnvironment("MONGO_INITDB_ROOT_USERNAME", "chronicle-test")
            .WithEnvironment("MONGO_INITDB_ROOT_PASSWORD", "chronicle-test")
            .WithEnvironment("GLIBC_TUNABLES", "glibc.pthread.rseq=1")
            .WithNetwork(_network)
            .WithNetworkAliases("mongodb")
            .WithPortBinding(27017, assignRandomHostPort: true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("mongosh", "--quiet", "--eval", "db.adminCommand({ ping: 1 })"))
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
    /// Gets a value indicating whether the most recently started kernel exited.
    /// </summary>
    public bool Exited { get; private set; }

    /// <summary>
    /// Starts the isolated database.
    /// </summary>
    /// <returns>The asynchronous operation.</returns>
    public async Task StartDatabase()
    {
        await _network.CreateAsync();
        await _mongoDB.StartAsync();
    }

    /// <summary>
    /// Starts a kernel and waits until it is healthy and its event store is populated.
    /// </summary>
    /// <returns>The asynchronous operation.</returns>
    /// <exception cref="TimeoutException">No event store was populated in time.</exception>
    public async Task StartHealthy()
    {
        var kernel = Build(Wait.ForUnixContainer().AddCustomWaitStrategy(new HttpsHealthWait(35000), options => options.WithTimeout(_readinessTimeout)));
        try
        {
            await kernel.StartAsync();
        }
        catch (TimeoutException)
        {
            throw new TimeoutException(await Describe(kernel));
        }
        Exited = false;
        var deadline = DateTimeOffset.UtcNow + _populateTimeout;
        while (!await StoreIsPopulated())
        {
            if (DateTimeOffset.UtcNow > deadline)
            {
                throw new TimeoutException("The kernel became healthy but no event store database was populated.");
            }
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }

    /// <summary>
    /// Starts a kernel without any readiness wait and waits, bounded, for it to exit.
    /// </summary>
    /// <returns>The asynchronous operation.</returns>
    public async Task StartExpectingRefusal()
    {
        var kernel = Build(Wait.ForUnixContainer());
        try
        {
            await kernel.StartAsync();
        }
        catch (ContainerNotRunningException)
        {
            Exited = true;
            return;
        }
        var deadline = DateTimeOffset.UtcNow + _exitTimeout;
        while (DateTimeOffset.UtcNow < deadline && !Exited)
        {
            Exited = await IsExited(kernel);
            if (!Exited)
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }
    }

    /// <summary>
    /// Stops every running kernel.
    /// </summary>
    /// <returns>The asynchronous operation.</returns>
    public async Task StopKernel()
    {
        foreach (var kernel in _kernels.Where(kernel => kernel.State == TestcontainersStates.Running))
        {
            await kernel.StopAsync();
        }
    }

    /// <summary>
    /// Determines whether the most recent kernel's output contains a text. The output is never exposed.
    /// </summary>
    /// <param name="text">The text to search for.</param>
    /// <returns>True if it is present on stdout or stderr.</returns>
    public async Task<bool> LogsContain(string text)
    {
        var (stdout, stderr) = await _kernels[^1].GetLogsAsync();
        return stdout.Contains(text, StringComparison.Ordinal) || stderr.Contains(text, StringComparison.Ordinal);
    }

    /// <summary>
    /// Seeds a durable generation floor above any released binary's support into every event-store database.
    /// </summary>
    /// <returns>The names of the seeded event-store databases.</returns>
    /// <exception cref="InvalidOperationException">The floor could not be seeded.</exception>
    public async Task<IReadOnlyList<string>> SeedFloorInEveryStore()
    {
        var client = Client;
        using var names = await client.ListDatabaseNamesAsync();
        var stores = (await names.ToListAsync()).Where(name => name.EndsWith("+es", StringComparison.Ordinal)).ToArray();
        var target = new GenerationFloor(new(2), new(2), [GenerationCapability.FloorAdmission], new(Guid.NewGuid()), 0, DateTimeOffset.UtcNow, "spec", "seeded above the released binary");
        foreach (var store in stores)
        {
            var storage = new GenerationFloorStorage(client.GetDatabase(store).GetCollection<BsonDocument>("generation-floor"));
            var result = await storage.Advance(new(target, 0, "spec", "seeded above the released binary", new(true, "spec")));
            if (!result.IsT0)
            {
                throw new InvalidOperationException("The generation floor could not be seeded.");
            }
        }
        return stores;
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        List<Exception> errors = [];
        foreach (var kernel in _kernels)
        {
            try
            {
                await kernel.DisposeAsync();
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
        }
        try
        {
            await _mongoDB.DisposeAsync();
        }
        catch (Exception error)
        {
            errors.Add(error);
        }
        try
        {
            await _network.DisposeAsync();
        }
        catch (Exception error)
        {
            errors.Add(error);
        }
        if (errors.Count > 0)
        {
            throw new AggregateException("Released kernel fixture cleanup failed.", errors);
        }
    }

    async Task<string> Describe(IContainer kernel)
    {
        // Booleans only: container output is searched, never printed.
        var (stdout, stderr) = await kernel.GetLogsAsync();
        var output = stdout + stderr;
        var hints = new[] { "floor", "Exception", "refus", "Unhealthy", "Now listening", "Application started", "authentication", "MongoDB" }
            .Select(text => $"{text}={output.Contains(text, StringComparison.OrdinalIgnoreCase)}");
        return $"Kernel not healthy. exited={await IsExited(kernel)} outputLength={output.Length} {string.Join(' ', hints)}";
    }

    async Task<bool> StoreIsPopulated()
    {
        var client = Client;
        using var names = await client.ListDatabaseNamesAsync();
        foreach (var name in (await names.ToListAsync()).Where(name => name.EndsWith("+es", StringComparison.Ordinal)))
        {
            var database = client.GetDatabase(name);
            using var collections = await database.ListCollectionNamesAsync();
            foreach (var collection in await collections.ToListAsync())
            {
                if (await database.GetCollection<BsonDocument>(collection).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty, new CountOptions { Limit = 1 }) > 0)
                {
                    return true;
                }
            }
        }
        return false;
    }

    static async Task<bool> IsExited(IContainer kernel)
    {
        // Only the status field is read; the container configuration is never printed.
        using var inspect = Process.Start(new ProcessStartInfo("docker", ["inspect", "--format", "{{.State.Status}}", kernel.Id]) { RedirectStandardOutput = true, UseShellExecute = false });
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try
        {
            var status = (await inspect.StandardOutput.ReadToEndAsync(timeout.Token)).Trim();
            await inspect.WaitForExitAsync(timeout.Token);
            return string.Equals(status, "exited", StringComparison.Ordinal) || string.Equals(status, "dead", StringComparison.Ordinal);
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            if (!inspect.HasExited)
            {
                inspect.Kill(entireProcessTree: true);
            }
            await inspect.WaitForExitAsync(CancellationToken.None);
            throw new TimeoutException("Docker status inspection exceeded its deadline.");
        }
    }

    IContainer Build(IWaitForContainerOS wait)
    {
        var kernel = new ContainerBuilder(_image)
            .WithNetwork(_network)
            .WithEnvironment(ReleasedKernelFixture.StorageTypeKey, "MongoDB")
            .WithEnvironment(ReleasedKernelFixture.StorageConnectionKey, "mongodb://chronicle-test:chronicle-test@mongodb:27017/?authSource=admin")
            .WithCreateParameterModifier(ReleasedKernelFixture.ReserveKernelPorts)
            .WithPortBinding(35000, true)
            .WithWaitStrategy(wait)
            .Build();
        _kernels.Add(kernel);
        return kernel;
    }
}
