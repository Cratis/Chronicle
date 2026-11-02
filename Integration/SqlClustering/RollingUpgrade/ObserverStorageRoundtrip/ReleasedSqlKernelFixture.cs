// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Cratis.Chronicle.Connections;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

namespace Cratis.Chronicle.Integration.SqlClustering.RollingUpgrade.ObserverStorageRoundtrip;

/// <summary>
/// Runs the released writer against an isolated PostgreSQL store without connecting a current client.
/// </summary>
public sealed partial class ReleasedSqlKernelFixture : IAsyncDisposable
{
    static readonly TimeSpan _readinessTimeout = TimeSpan.FromMinutes(3);

    static readonly TimeSpan _containerDisposeTimeout = TimeSpan.FromSeconds(45);
    static readonly TimeSpan _networkDisposeTimeout = TimeSpan.FromSeconds(15);

    readonly INetwork _network;
    readonly string? _run;
    readonly ConcurrentDictionary<string, string> _containerIds = new();
    readonly IContainer _kernel;
    readonly ReleasedSqlObserverReadyWait _ready;
    IContainer? _current;
    bool _disposed;

    public ChronicleConnectionString ConnectionString
    {
        get
        {
            var active = _current ?? _kernel;
            return new ChronicleConnectionString($"chronicle://{active.Hostname}:{active.GetMappedPublicPort(35000)}/?skipTlsValidation=true")
                .WithCredentials(ChronicleConnectionString.DevelopmentClient, ChronicleConnectionString.DevelopmentClientSecret);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReleasedSqlKernelFixture"/> class.
    /// </summary>
    /// <param name="traceStatements">Whether PostgreSQL logs every statement for the upgrade proof.</param>
    /// <param name="run">The optional run identifier that names and labels every resource so an external supervisor can address it.</param>
    /// <exception cref="ArgumentException">The run identifier is not a lower-case DNS-style token.</exception>
    public ReleasedSqlKernelFixture(bool traceStatements = false, string? run = null)
    {
        if (run is not null && !RunPattern.IsMatch(run))
        {
            throw new ArgumentException("The run identifier must match ^[a-z0-9][a-z0-9-]{7,40}$.", nameof(run));
        }

        _run = run;
        var network = new NetworkBuilder();
        if (run is not null)
        {
            network = network.WithName($"{run}-network").WithLabel(SqlClusterDatabase.RunLabel, run);
        }

        _network = network.Build();
        Database = new(true, _network, traceStatements, run);
        _ready = new(Database);
        var kernel = new ContainerBuilder("cratis/chronicle:19.106.2-development-slim@sha256:3dcf0cf274200619809573faa16395195364568a1162d9c210e815b5ba5a9643")
            .WithNetwork(_network)
            .WithEnvironment("Cratis__Chronicle__Storage__Type", "PostgreSql")
            .WithEnvironment("Cratis__Chronicle__Storage__ConnectionDetails", Database.KernelConnection)
            .WithEnvironment("Cratis__Chronicle__Clustering__Type", "Sql")

            // The released kernel predates the dedicated health port: it serves /health over HTTPS on 35000 only.
            // Every wait is bounded so a readiness mismatch fails the spec instead of hanging the test host.
            .WithPortBinding(35000, true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .AddCustomWaitStrategy(new HttpsHealthWait(35000), options => options.WithTimeout(_readinessTimeout))
                .AddCustomWaitStrategy(_ready, options => options.WithTimeout(_readinessTimeout)));
        _kernel = Named(kernel, "released");
    }

    /// <summary>
    /// Gets the isolated test database.
    /// </summary>
    public SqlClusterDatabase Database { get; }

    /// <summary>
    /// Gets the persisted old-kernel observer selected by the readiness signal.
    /// </summary>
    /// <exception cref="InvalidOperationException">The old kernel is not ready.</exception>
    public (string ConnectionString, string Identifier, string Owner) Observer => _ready.Observer ?? throw new InvalidOperationException("Old kernel is not ready.");

    /// <summary>
    /// Starts PostgreSQL and the old writer.
    /// </summary>
    /// <param name="token">The scenario deadline.</param>
    /// <returns>The asynchronous operation.</returns>
    public async Task Start(CancellationToken token = default)
    {
        await _network.CreateAsync(token);
        await Database.Start(token);
        await _kernel.StartAsync(token);
    }

    /// <summary>
    /// Stops the old writer while the new provider migrates and augments its state.
    /// </summary>
    /// <returns>The asynchronous operation.</returns>
    public Task StopKernel() => _kernel.StopAsync();

    /// <summary>
    /// Restarts the old writer against the augmented schema.
    /// </summary>
    /// <returns>The asynchronous operation.</returns>
    public Task RestartKernel() => _kernel.StartAsync();

    public async Task StartCurrent(string image, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(image) || _current is not null)
        {
            throw new InvalidOperationException("Supply one provenance-checked current image; replacement is one-way.");
        }

        await _kernel.StopAsync(token);
        var current = new ContainerBuilder(image)
            .WithNetwork(_network)
            .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
            .WithEnvironment("Cratis__Chronicle__Storage__Type", "PostgreSql")
            .WithEnvironment("Cratis__Chronicle__Storage__ConnectionDetails", Database.KernelConnection)
            .WithEnvironment("Cratis__Chronicle__Clustering__Type", "Sql")
            .WithPortBinding(35000, true)
            .WithWaitStrategy(Wait.ForUnixContainer().AddCustomWaitStrategy(
                new HttpsHealthWait(35000), options => options.WithTimeout(_readinessTimeout)));
        _current = Named(current, "current");
        await _current.StartAsync(token);
    }

    /// <summary>
    /// Disposes every resource concurrently within fixed bounds and reports every failure together.
    /// </summary>
    /// <returns>The asynchronous operation.</returns>
    /// <exception cref="AggregateException">One or more resources failed or timed out being disposed.</exception>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        var containers = new List<Task<Exception?>>
        {
            Bounded("released kernel", _kernel.DisposeAsync(), _containerDisposeTimeout),
            Bounded("database", Database.DisposeAsync(), _containerDisposeTimeout)
        };
        if (_current is not null) containers.Add(Bounded("current kernel", _current.DisposeAsync(), _containerDisposeTimeout));

        var failures = (await Task.WhenAll(containers)).ToList();
        failures.Add(await Bounded("network", _network.DisposeAsync(), _networkDisposeTimeout));
        var errors = failures.OfType<Exception>().ToArray();
        if (errors.Length > 0)
        {
            var known = string.Join(", ", _containerIds.Select(pair => $"{pair.Key}={pair.Value}"));
            throw new AggregateException($"Disposing run '{_run ?? "(unnamed)"}' failed; known container ids: [{known}].", errors);
        }
    }

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{7,40}$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex RunPattern { get; }

    static async Task<Exception?> Bounded(string resource, ValueTask dispose, TimeSpan limit)
    {
        try
        {
            await dispose.AsTask().WaitAsync(limit);
            return null;
        }
        catch (Exception exception)
        {
            return new InvalidOperationException($"Disposing the {resource} failed or exceeded {limit.TotalSeconds:0} s.", exception);
        }
    }

    IContainer Named(ContainerBuilder builder, string role)
    {
        if (_run is not null)
        {
            builder = builder.WithName($"{_run}-{role}").WithLabel(SqlClusterDatabase.RunLabel, _run);
        }

        var container = builder.Build();
        container.Created += (_, _) => _containerIds[role] = container.Id;
        return container;
    }
}
