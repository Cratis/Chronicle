// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

namespace Cratis.Chronicle.Chaos;

/// <summary>
/// Owns the dedicated kernel, storage, and ledger containers for a run.
/// </summary>
internal sealed class DockerEnvironment : IAsyncDisposable
{
    readonly INetwork _network;
    readonly IContainer _ledger;
    readonly bool _keep;

    /// <summary>
    /// Initializes a new instance of the <see cref="DockerEnvironment"/> class.
    /// </summary>
    /// <param name="runId">The isolated run identifier.</param>
    /// <param name="keep">Whether to retain containers after the run.</param>
    public DockerEnvironment(string runId, bool keep)
    {
        RunId = runId;
        _keep = keep;
        _network = new NetworkBuilder().WithName($"chronicle-chaos-{runId}").WithLabel("chronicle-chaos", runId).WithCleanUp(false).Build();
        Mongo = MongoContainer(runId, "mongo", replicaSet: true);
        _ledger = MongoContainer(runId, "ledger", replicaSet: false);
        Kernel = new ContainerBuilder("cratis/chronicle:local-development-slim")
            .WithNetwork(_network).WithNetworkAliases("kernel").WithPortBinding(35000, true)
            .WithCreateParameterModifier(parameters =>
            {
                foreach (var binding in parameters.HostConfig?.PortBindings?.Values.SelectMany(bindings => bindings) ?? []) binding.HostIP = "127.0.0.1";
            })
            .WithLabel("chronicle-chaos", runId).WithCleanUp(false).WithAutoRemove(false)
            .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
            .WithEnvironment("Cratis__Chronicle__Storage__Type", "MongoDB")
            .WithEnvironment("Cratis__Chronicle__Storage__ConnectionDetails", "mongodb://mongo:27017/?replicaSet=rs0")
            .WithEnvironment("Cratis__Chronicle__Clustering__Type", "Localhost")
            .WithEnvironment("Cratis__Chronicle__Clustering__SingleWriterLease__Ttl", "00:00:05")
            .WithEnvironment("Cratis__Chronicle__Authentication__Enabled", "false")
            .WithEnvironment("Cratis__Chronicle__Tls__Enabled", "false")
            .WithEnvironment("Cratis__Chronicle__Health__Tls", "false")
            .Build();
    }

    /// <summary>
    /// Gets the isolated run identifier.
    /// </summary>
    public string RunId { get; }

    /// <summary>
    /// Gets the fault-target MongoDB container.
    /// </summary>
    public IContainer Mongo { get; }

    /// <summary>
    /// Gets the fault-target kernel container.
    /// </summary>
    public IContainer Kernel { get; }

    /// <summary>
    /// Gets the plaintext kernel connection string.
    /// </summary>
    public string Connection => $"chronicle://{Kernel.Hostname}:{Kernel.GetMappedPublicPort(35000)}/?development=true&disableTls=true";

    /// <summary>
    /// Gets the kernel storage connection string.
    /// </summary>
    public string MongoConnection => $"mongodb://{Mongo.Hostname}:{Mongo.GetMappedPublicPort(27017)}/?directConnection=true&replicaSet=rs0";

    /// <summary>
    /// Gets the independent ledger connection string.
    /// </summary>
    public string LedgerConnection => $"mongodb://{_ledger.Hostname}:{_ledger.GetMappedPublicPort(27017)}/?directConnection=true";

    /// <summary>
    /// Starts storage and the kernel and waits for readiness.
    /// </summary>
    /// <param name="cancellation">The startup cancellation token.</param>
    /// <returns>The startup task.</returns>
    /// <exception cref="InvalidOperationException">MongoDB replica-set initiation failed.</exception>
    public async Task Start(CancellationToken cancellation)
    {
        await _network.CreateAsync(cancellation);
        await _ledger.StartAsync(cancellation);
        await Mongo.StartAsync(cancellation);
        var initiated = await Mongo.ExecAsync(["mongosh", "--quiet", "--eval", "rs.initiate({_id:'rs0',members:[{_id:0,host:'mongo:27017'}]})"], cancellation);
        if (initiated.ExitCode != 0) throw new InvalidOperationException($"Mongo replica-set initiation failed: {initiated.Stderr}");
        await WaitForPrimary(cancellation);
        await Kernel.StartAsync(cancellation);
        await WaitForKernel(cancellation);
    }

    /// <summary>
    /// Waits for the kernel's HTTP/2 readiness endpoint.
    /// </summary>
    /// <param name="cancellation">The readiness cancellation token.</param>
    /// <returns>The readiness task.</returns>
    public async Task WaitForKernel(CancellationToken cancellation)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        deadline.CancelAfter(TimeSpan.FromSeconds(150));
        using var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(3),
            DefaultRequestVersion = HttpVersion.Version20,
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact
        };
        while (true)
        {
            try
            {
                using var response = await client.GetAsync($"http://{Kernel.Hostname}:{Kernel.GetMappedPublicPort(35000)}/health/ready", deadline.Token);
                if (response.IsSuccessStatusCode) return;
            }
            catch (HttpRequestException) { }
            catch (OperationCanceledException) when (!deadline.IsCancellationRequested) { }
            await Task.Delay(TimeSpan.FromSeconds(1), deadline.Token);
        }
    }

    /// <summary>
    /// Captures output from every container that was created.
    /// </summary>
    /// <param name="directory">The output directory.</param>
    /// <param name="cancellation">The capture cancellation token.</param>
    /// <returns>The capture task.</returns>
    public async Task CaptureLogs(string directory, CancellationToken cancellation)
    {
        foreach (var (name, container) in new[] { ("kernel", Kernel), ("mongo", Mongo), ("ledger", _ledger) })
        {
            await CaptureLogsFor(name, async () =>
            {
                var logs = await container.GetLogsAsync(ct: cancellation);
                await File.WriteAllTextAsync(Path.Combine(directory, $"{name}.stdout.log"), logs.Stdout, cancellation);
                await File.WriteAllTextAsync(Path.Combine(directory, $"{name}.stderr.log"), logs.Stderr, cancellation);
            });
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_keep) return;

        // Label cleanup also handles partially started containers and does not depend on their running state.
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await Cleanup.Run(RunId, deadline.Token);
        await Mongo.DisposeAsync();
        await _ledger.DisposeAsync();
        await Kernel.DisposeAsync();
        await _network.DisposeAsync();
    }

    /// <summary>
    /// Captures diagnostics without masking a workload or startup failure.
    /// </summary>
    /// <param name="name">The diagnostic source name.</param>
    /// <param name="capture">The best-effort capture operation.</param>
    /// <returns>The diagnostic capture task.</returns>
    internal static async Task CaptureLogsFor(string name, Func<Task> capture)
    {
        try
        {
            await capture();
        }
        catch (Exception exception)
        {
            await Console.Error.WriteLineAsync($"Could not capture {name} logs: {exception.Message}");
        }
    }

    IContainer MongoContainer(string runId, string alias, bool replicaSet)
    {
        var builder = new ContainerBuilder("mongo:8.0").WithNetwork(_network).WithNetworkAliases(alias)
            .WithPortBinding(27017, true).WithLabel("chronicle-chaos", runId).WithCleanUp(false).WithAutoRemove(false)
            .WithCreateParameterModifier(parameters =>
            {
                foreach (var binding in parameters.HostConfig?.PortBindings?.Values.SelectMany(bindings => bindings) ?? []) binding.HostIP = "127.0.0.1";
            })
            .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("mongosh", "--quiet", "--eval", "db.adminCommand({ping:1})"));
        if (replicaSet) builder = builder.WithCommand("mongod", "--replSet", "rs0", "--bind_ip_all");
        return builder.Build();
    }

    async Task WaitForPrimary(CancellationToken cancellation)
    {
        while (true)
        {
            var result = await Mongo.ExecAsync(["mongosh", "--quiet", "--eval", "if (!db.hello().isWritablePrimary) quit(1)"], cancellation);
            if (result.ExitCode == 0) return;
            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellation);
        }
    }
}
