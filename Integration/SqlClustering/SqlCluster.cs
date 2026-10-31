// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using Cratis.Chronicle.Connections;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

namespace Cratis.Chronicle.Integration.SqlClustering;

public sealed class SqlCluster(bool postgres, string? image = null) : IAsyncDisposable
{
    /// <summary>
    /// The image of the build under test, from the environment when CI supplies one.
    /// </summary>
    public static readonly string CurrentImage = Environment.GetEnvironmentVariable("CRATIS_CHRONICLE_LOCAL_IMAGE") ?? "cratis/chronicle:latest-development";

    readonly string _image = image ?? CurrentImage;
    readonly INetwork _network = new NetworkBuilder().Build();
    readonly List<IContainer> _nodes = [];
    readonly List<int> _ports = [];
    readonly SqlClusterLogs _logs = new();
    readonly string _id = $"sql-clustering-{Guid.NewGuid():N}";
    SqlClusterDatabase _database = null!;

    public IReadOnlyList<IContainer> Nodes => _nodes;

    public ChronicleConnectionString ConnectionString => new ChronicleConnectionString(
        $"chronicle://{string.Join(',', _nodes.Select((node, index) => $"{node.Hostname}:{_ports[index]}"))}/?skipTlsValidation=true")
        .WithCredentials(ChronicleConnectionString.DevelopmentClient, ChronicleConnectionString.DevelopmentClientSecret);

    public async Task Start()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(6));
        await _network.CreateAsync(timeout.Token);
        _database = new(postgres, _network);
        await _database.Start(timeout.Token);
        _ports.AddRange(Enumerable.Range(0, 3).Select(_ => FreePort()));
        _nodes.AddRange(Enumerable.Range(0, 3).Select(index => BuildNode(index, _image)));
        await Task.WhenAll(_nodes.Select(node => node.StartAsync(timeout.Token)));
        await WaitForMembership(entries => entries.Count(entry => entry.Status == (int)SiloStatus.Active) == 3, TimeSpan.FromMinutes(1));
    }

    /// <summary>
    /// Replaces a node with a container running another image, the way a rolling deployment replaces a pod: the old
    /// silo leaves gracefully and a new one joins under the same host name and client port.
    /// </summary>
    /// <param name="index">The node to replace.</param>
    /// <param name="replacementImage">The image the replacement runs.</param>
    /// <returns>Awaitable task.</returns>
    public async Task Replace(int index, string replacementImage)
    {
        var previous = _nodes[index];
        await previous.StopAsync().WaitAsync(TimeSpan.FromSeconds(90));
        await _logs.CaptureReplaced(previous);
        await previous.DisposeAsync();

        var replacement = BuildNode(index, replacementImage);
        _nodes[index] = replacement;
        await replacement.StartAsync().WaitAsync(TimeSpan.FromMinutes(3));
    }

    public async Task<IReadOnlyList<MembershipEntry>> WaitForMembership(Func<IReadOnlyList<MembershipEntry>, bool> predicate, TimeSpan timeout)
    {
        using var deadline = new CancellationTokenSource(timeout);

        // Infrastructure readiness backoff: query the real SQL membership rather than inferring it from health.
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(200));
        do
        {
            var entries = await _database.Membership(_id, deadline.Token);
            if (predicate(entries))
            {
                return entries;
            }
        }
        while (await timer.WaitForNextTickAsync(deadline.Token));

        throw new TimeoutException("SQL membership did not converge.");
    }

    public Task DumpLogs() => _logs.Dump(_nodes, Console.Out);

    public async ValueTask DisposeAsync()
    {
        foreach (var node in _nodes)
        {
            await node.DisposeAsync();
        }

        if (_database is not null)
        {
            await _database.DisposeAsync();
        }

        await _network.DisposeAsync();
    }

    IContainer BuildNode(int index, string nodeImage) => new ContainerBuilder(nodeImage)
        .WithHostname($"sql-node-{index}")
        .WithCreateParameterModifier(parameters => parameters.StopTimeout = TimeSpan.FromSeconds(90))
        .WithNetwork(_network)

        // A fixed host port survives a container restart, so the client keeps reaching every node through one connection string.
        .WithPortBinding(_ports[index], 35000)

        // Resolve the address inside each container; AdvertisedIP accepts an IP literal, not a DNS name.
        .WithEntrypoint(
            "/usr/bin/tini",
            "--",
            "/bin/bash",
            "-c",
            "export Cratis__Chronicle__Clustering__AdvertisedIP=$(hostname -i); exec /app/entrypoint.sh")
        .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
        .WithEnvironment("Cratis__Chronicle__Storage__Type", postgres ? "PostgreSql" : "MsSql")
        .WithEnvironment("Cratis__Chronicle__Storage__ConnectionDetails", _database.KernelConnection)
        .WithEnvironment("Cratis__Chronicle__Clustering__Type", "Sql")
        .WithEnvironment("Cratis__Chronicle__Clustering__ClusterId", _id)
        .WithEnvironment("Cratis__Chronicle__Clustering__ServiceId", _id)
        .WithWaitStrategy(Wait.ForUnixContainer().AddCustomWaitStrategy(
            new HttpsHealthWait(35000), options => options.WithRetries(300).WithInterval(TimeSpan.FromSeconds(1))))
        .Build();

    static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
