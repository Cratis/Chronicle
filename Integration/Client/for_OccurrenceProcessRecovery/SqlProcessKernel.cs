// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using Cratis.Chronicle.Integration.for_PerProcessActivation;
using Docker.DotNet;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Testcontainers.PostgreSql;

namespace Cratis.Chronicle.Integration.for_OccurrenceProcessRecovery;

public sealed class SqlProcessKernel : IAsyncDisposable
{
    readonly INetwork _network = new NetworkBuilder().Build();
    readonly List<ChildProcess> _children = [];
    readonly KernelFixtureCertificate _certificate = KernelFixtureCertificate.Create();
    readonly DockerClient _docker = new DockerClientBuilder().Build();
    PostgreSqlContainer? _database;
    IContainer? _kernel;
    string _databaseStartedAt = string.Empty;
    string _kernelStartedAt = string.Empty;

    public string Store { get; } = $"process-recovery-{Guid.NewGuid():N}";
    public string Connection { get; private set; } = string.Empty;

    public async Task Start()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(180));
        await _network.CreateAsync(deadline.Token);
        var password = Guid.NewGuid().ToString("N");
        _database = new PostgreSqlBuilder("postgres:17-alpine")
            .WithNetwork(_network).WithNetworkAliases("database")
            .WithDatabase("chronicle").WithUsername("chronicle").WithPassword(password).Build();
        await _database.StartAsync(deadline.Token);
        var port = FreePort();
        var image = Environment.GetEnvironmentVariable("CRATIS_CHRONICLE_LOCAL_IMAGE")
            ?? throw new InvalidOperationException("Set CRATIS_CHRONICLE_LOCAL_IMAGE to the isolated development-slim image built for this run.");
        _kernel = new ContainerBuilder(image)
            .WithNetwork(_network).WithPortBinding(port, 35000)
            .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
            .WithEnvironment("Cratis__Chronicle__Storage__Type", "PostgreSql")
            .WithEnvironment("Cratis__Chronicle__Storage__ConnectionDetails", $"Host=database;Port=5432;Database=chronicle;Username=chronicle;Password={password}")
            .WithEnvironment("Cratis__Chronicle__Clustering__Type", "Localhost")
            .WithEnvironment("Cratis__Chronicle__Clustering__SingleWriterLease__Ttl", "00:00:05")
            .WithEnvironment("Cratis__Chronicle__Tls__CertificatePath", "/certs/kernel.pfx")
            .WithBindMount(_certificate.Path, "/certs/kernel.pfx", DotNet.Testcontainers.Configurations.AccessMode.ReadOnly)
            .WithWaitStrategy(Wait.ForUnixContainer().AddCustomWaitStrategy(new HttpsHealthWait(35000), options => options.WithTimeout(TimeSpan.FromSeconds(150))))
            .Build();
        await _kernel.StartAsync(deadline.Token);
        Connection = $"chronicle://{_kernel.Hostname}:{port}/?development=true&certificateHash={_certificate.Hash}";
        _databaseStartedAt = (await Inspect(_database.Id)).State.StartedAt;
        _kernelStartedAt = (await Inspect(_kernel.Id)).State.StartedAt;
    }

    public async Task<ChildProcess> StartApplication(bool hold)
    {
        var child = ChildProcess.Start(Connection, Store, hold ? OccurrenceApplication.HolderVerb : OccurrenceApplication.RecoveryVerb);
        _children.Add(child);
        await child.WaitForLine("READY", TimeSpan.FromSeconds(90));
        return child;
    }

    public async Task KillKernel()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await _docker.Containers.KillContainerAsync(_kernel!.Id, new ContainerKillParameters { Signal = "SIGKILL" }, deadline.Token);
        var exit = await _docker.Containers.WaitContainerAsync(_kernel.Id, deadline.Token);
        exit.StatusCode.ShouldEqual(137L);
        (await Inspect(_kernel.Id)).State.Running.ShouldBeFalse();
        await AssertDatabaseAlive();
    }

    public async Task RestartKernel()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(180));
        await _kernel!.StartAsync(deadline.Token);
        var state = (await Inspect(_kernel.Id)).State;
        state.Running.ShouldBeTrue();
        state.StartedAt.ShouldNotEqual(_kernelStartedAt);
        await AssertDatabaseAlive();
    }

    public async Task AssertKernelUnchanged()
    {
        var state = (await Inspect(_kernel!.Id)).State;
        state.Running.ShouldBeTrue();
        state.StartedAt.ShouldEqual(_kernelStartedAt);
        await AssertDatabaseAlive();
    }

    public async ValueTask DisposeAsync()
    {
        var failures = new List<Exception>();
        foreach (var child in _children) await Cleanup(() => child.DisposeAsync().AsTask(), failures);
        Exception? kernelFailure = null;
        try
        {
            if (_kernel is not null) await _kernel.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(45));
        }
        catch (Exception exception) { kernelFailure = exception; }
        await Cleanup(
            () => _certificate.DisposeAfter(() => kernelFailure is null ? ValueTask.CompletedTask : ValueTask.FromException(kernelFailure)).AsTask(),
            failures);
        try
        {
            if (_database is not null) await _database.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(45));
        }
        catch (Exception exception) { failures.Add(exception); }
        try { await _network.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(30)); }
        catch (Exception exception) { failures.Add(exception); }
        try { _docker.Dispose(); }
        catch (Exception exception) { failures.Add(exception); }
        if (failures.Count > 0) throw new AggregateException("Process recovery fixture cleanup failed.", failures);
    }

    async Task AssertDatabaseAlive()
    {
        var state = (await Inspect(_database!.Id)).State;
        state.Running.ShouldBeTrue();
        state.StartedAt.ShouldEqual(_databaseStartedAt);
    }

    async Task<ContainerInspectResponse> Inspect(string id)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        return await _docker.Containers.InspectContainerAsync(id, deadline.Token);
    }

    static async Task Cleanup(Func<Task> dispose, List<Exception> failures)
    {
        try { await dispose(); }
        catch (Exception exception) { failures.Add(exception); }
    }

    static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
