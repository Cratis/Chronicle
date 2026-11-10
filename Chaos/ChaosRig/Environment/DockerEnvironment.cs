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
    readonly HostPortReservation _mongoPort = new();
    readonly HostPortReservation _ledgerPort = new();
    readonly HostPortReservation _kernelPort = new();
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
        Mongo = MongoContainer(runId, "mongo", _mongoPort, replicaSet: true);
        _ledger = MongoContainer(runId, "ledger", _ledgerPort, replicaSet: false);
        Kernel = new ContainerBuilder("cratis/chronicle:local-development-slim")
            .WithNetwork(_network).WithNetworkAliases("kernel").WithPortBinding(_kernelPort.Port, 35000)
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
    public string Connection => $"chronicle://127.0.0.1:{_kernelPort.Port}/?development=true&disableTls=true";

    /// <summary>
    /// Gets the kernel storage connection string.
    /// </summary>
    public string MongoConnection => $"mongodb://127.0.0.1:{_mongoPort.Port}/?directConnection=true&replicaSet=rs0";

    /// <summary>
    /// Gets the independent ledger connection string.
    /// </summary>
    public string LedgerConnection => $"mongodb://127.0.0.1:{_ledgerPort.Port}/?directConnection=true";

    /// <summary>
    /// Starts storage and the kernel and waits for readiness.
    /// </summary>
    /// <param name="cancellation">The startup cancellation token.</param>
    /// <returns>The startup task.</returns>
    /// <exception cref="InvalidOperationException">MongoDB replica-set initiation failed.</exception>
    public async Task Start(CancellationToken cancellation)
    {
        await _network.CreateAsync(cancellation);
        _ledgerPort.Dispose();
        await _ledger.StartAsync(cancellation).WaitAsync(cancellation);
        await LogMapping(_ledger, "started", verify: true, cancellation);
        _mongoPort.Dispose();
        await Mongo.StartAsync(cancellation).WaitAsync(cancellation);
        await LogMapping(Mongo, "started", verify: true, cancellation);
        var initiated = await Mongo.ExecAsync(["mongosh", "--quiet", "--eval", "rs.initiate({_id:'rs0',members:[{_id:0,host:'mongo:27017'}]})"], cancellation);
        if (initiated.ExitCode != 0) throw new InvalidOperationException($"Mongo replica-set initiation failed: {initiated.Stderr}");
        await WaitForPrimary(cancellation);
        _kernelPort.Dispose();
        await Kernel.StartAsync(cancellation).WaitAsync(cancellation);
        await LogMapping(Kernel, "started", verify: true, cancellation);
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
                using var response = await client.GetAsync($"http://127.0.0.1:{_kernelPort.Port}/health/ready", deadline.Token);
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
                var logs = await container.GetLogsAsync(ct: cancellation).WaitAsync(cancellation);
                await File.WriteAllTextAsync(Path.Combine(directory, $"{name}.stdout.log"), logs.Stdout, cancellation);
                await File.WriteAllTextAsync(Path.Combine(directory, $"{name}.stderr.log"), logs.Stderr, cancellation);
            });
        }
    }

    /// <summary>
    /// Restarts a container while recording and validating its stable loopback mapping.
    /// </summary>
    /// <param name="container">The owned container to restart.</param>
    /// <param name="cancellation">The control-operation cancellation token.</param>
    /// <returns>The restart task.</returns>
    public async Task Restart(IContainer container, CancellationToken cancellation)
    {
        await LogMapping(container, "before restart", verify: false, cancellation);
        await ContainerSignals.Restart(container.Id, cancellation);
        await LogMapping(container, "after restart", verify: true, cancellation);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        _mongoPort.Dispose();
        _ledgerPort.Dispose();
        _kernelPort.Dispose();
        if (_keep) return;

        // Label cleanup also handles partially started containers and does not depend on their running state.
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await Cleanup.Run(RunId, deadline.Token).WaitAsync(deadline.Token);
        await Mongo.DisposeAsync().AsTask().WaitAsync(deadline.Token);
        await _ledger.DisposeAsync().AsTask().WaitAsync(deadline.Token);
        await Kernel.DisposeAsync().AsTask().WaitAsync(deadline.Token);
        await _network.DisposeAsync().AsTask().WaitAsync(deadline.Token);
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

    /// <summary>
    /// Checks the actual Docker mapping against the port selected for the run.
    /// </summary>
    /// <param name="mapping">The output from docker port.</param>
    /// <param name="containerPort">The port inside the container.</param>
    /// <param name="hostPort">The stable loopback host port.</param>
    /// <returns>Whether Docker reports the expected loopback-only mapping.</returns>
    internal static bool MappingMatches(string mapping, int containerPort, int hostPort)
    {
        var lines = mapping.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return lines.Length == 1 && lines[0] == $"{containerPort}/tcp -> 127.0.0.1:{hostPort}";
    }

    IContainer MongoContainer(string runId, string alias, HostPortReservation port, bool replicaSet)
    {
        var builder = new ContainerBuilder("mongo:8.0").WithNetwork(_network).WithNetworkAliases(alias)
            .WithPortBinding(port.Port, 27017).WithLabel("chronicle-chaos", runId).WithCleanUp(false).WithAutoRemove(false)
            .WithCreateParameterModifier(parameters =>
            {
                foreach (var binding in parameters.HostConfig?.PortBindings?.Values.SelectMany(bindings => bindings) ?? []) binding.HostIP = "127.0.0.1";
            })
            .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("mongosh", "--quiet", "--eval", "db.adminCommand({ping:1})"));
        if (replicaSet) builder = builder.WithCommand("mongod", "--replSet", "rs0", "--bind_ip_all");
        return builder.Build();
    }

    async Task LogMapping(IContainer container, string stage, bool verify, CancellationToken cancellation)
    {
        var mapping = await NativeCommand.Run("docker", ["port", container.Id], cancellation);
        var (name, internalPort, hostPort) = MappingFor(container);
        Console.WriteLine($"PORT {stage} {name} expected=127.0.0.1:{hostPort} actual={mapping.Trim()}");
        if (verify && !MappingMatches(mapping, internalPort, hostPort)) throw new InvalidOperationException($"Unexpected {name} port mapping after {stage}.");
    }

    (string Name, int InternalPort, int HostPort) MappingFor(IContainer container)
    {
        if (container == Kernel) return ("kernel", 35000, _kernelPort.Port);
        if (container == Mongo) return ("mongo", 27017, _mongoPort.Port);
        return ("ledger", 27017, _ledgerPort.Port);
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
