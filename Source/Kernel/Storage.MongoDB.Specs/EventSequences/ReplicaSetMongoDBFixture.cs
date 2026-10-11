// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences;

/// <summary>
/// Provides a shared single-node replica-set MongoDB container for event sequence storage specs that
/// need transactions (multi-document writes go through a session transaction).
/// </summary>
public sealed class ReplicaSetMongoDBFixture : IAsyncLifetime
{
    const int MongoDBPort = 27017;
    internal const string StartupCommand = "mongod --replSet rs0 --bind_ip_all > /proc/1/fd/1 2>/proc/1/fd/2 & " +
        "until mongosh --quiet --eval 'db.adminCommand(\"ping\")' >/dev/null 2>&1; do sleep 0.1; done; " +
        "mongosh --eval 'const result = rs.initiate({_id:\"rs0\",members:[{_id:0,host:\"localhost:27017\"}]}); " +
        "if (result.ok !== 1) { throw new Error(\"rs.initiate returned: \" + JSON.stringify(result)); }'; " +
        "status=$?; if [ \"$status\" -ne 0 ]; then " +
        "echo \"MongoDB fixture rs.initiate failed (mongosh exit $status); see mongosh output above and mongod container logs.\" >&2; " +
        "exit \"$status\"; fi; tail -f /dev/null";
    static readonly TimeSpan _primaryReadinessTimeout = TimeSpan.FromSeconds(45);

    IContainer? _container;

    /// <summary>
    /// Gets the MongoDB connection string.
    /// </summary>
    public string ConnectionString => MongoDBSpecDatabaseNames.ExternalConnectionString
        ?? $"mongodb://localhost:{_container!.GetMappedPublicPort(MongoDBPort)}/?directConnection=true";

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        if (MongoDBSpecDatabaseNames.ExternalConnectionString is not null)
        {
            // External services own their topology; do not initiate a replica set or require its name to be rs0.
            return;
        }

        var image = Environment.GetEnvironmentVariable("CHRONICLE_SPECS_MONGODB_IMAGE") ?? "mongo:8.2";
        _container = new ContainerBuilder(image)
            .WithMongoDBKernelCompatibility()
            .WithCommand("/bin/sh", "-c", StartupCommand)
            .WithPortBinding(MongoDBPort, assignRandomHostPort: true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilInternalTcpPortIsAvailable(MongoDBPort))
            .Build();

        try
        {
            await _container.StartMongoDBWithDiagnostics();
            await WaitForPrimary();
        }
        catch (Exception startupError)
        {
            // Capture bounded diagnostics for every startup failure before disposing the container.
            // A TCP-ready mongod can still fail rs.initiate after Testcontainers reports the port ready.
            string? stdout = null;
            string? stderr = null;
            string? logRetrievalError = null;
            string? disposalError = null;
            try
            {
                using var logsTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                (stdout, stderr) = await _container.GetLogsAsync(ct: logsTimeout.Token);
                startupError.Data["MongoDB fixture stderr"] = Tail(stderr);
                startupError.Data["MongoDB fixture stdout"] = Tail(stdout);
            }
            catch (Exception logsError)
            {
                // Preserve the original startup error if Docker cannot retrieve logs.
                logRetrievalError = logsError.Message;
                startupError.Data["MongoDB fixture log retrieval error"] = logRetrievalError;
            }

            try
            {
                await _container.DisposeAsync();
            }
            catch (Exception error)
            {
                disposalError = error.Message;
                startupError.Data["MongoDB fixture disposal error"] = disposalError;
            }
            _container = null;
            var diagnosticError = WithStartupDiagnostics(startupError, stdout, stderr, logRetrievalError, disposalError);
            if (ReferenceEquals(diagnosticError, startupError))
            {
                throw;
            }
            throw diagnosticError;
        }
    }

    internal static Exception WithStartupDiagnostics(
        Exception startupError,
        string? stdout,
        string? stderr,
        string? logRetrievalError = null,
        string? disposalError = null)
    {
        var diagnostics = new List<string>();
        if (!string.IsNullOrEmpty(stderr))
        {
            diagnostics.Add($"MongoDB fixture stderr:\n{Tail(stderr)}");
        }
        if (!string.IsNullOrEmpty(stdout))
        {
            diagnostics.Add($"MongoDB fixture stdout:\n{Tail(stdout)}");
        }
        if (!string.IsNullOrEmpty(logRetrievalError))
        {
            diagnostics.Add($"MongoDB fixture log retrieval error: {logRetrievalError}");
        }
        if (!string.IsNullOrEmpty(disposalError))
        {
            diagnostics.Add($"MongoDB fixture disposal error: {disposalError}");
        }
        if (diagnostics.Count == 0)
        {
            return startupError;
        }

        var initiationFailed = stderr?.Contains("MongoDB fixture rs.initiate failed (mongosh exit", StringComparison.Ordinal) == true;
        var prefix = initiationFailed ? "MongoDB fixture rs.initiate failed" : "MongoDB fixture startup failed";
        return new InvalidOperationException($"{prefix}: {startupError.Message}\n{string.Join('\n', diagnostics)}", startupError);
    }

    static string Tail(string value) => value[^Math.Min(value.Length, 4096)..];

    internal static bool IsReady(BsonDocument hello) =>
        hello.TryGetValue("setName", out var setName) && setName.IsString && setName.AsString == "rs0" &&
        hello.TryGetValue("isWritablePrimary", out var primary) && primary.IsBoolean && primary.AsBoolean;

    async Task WaitForPrimary()
    {
        // Probe the mapped host address used by the specs, not only mongosh inside the container.
        var client = CreateReadinessClient(ConnectionString);
        try
        {
            await WaitForPrimary(cancellationToken => client.GetDatabase("admin").RunCommandAsync<BsonDocument>(
                new BsonDocument("hello", 1), cancellationToken: cancellationToken));
        }
        finally
        {
            DisposeReadinessClient(client);
        }
    }

    internal static MongoClient CreateReadinessClient(string connectionString)
    {
        var settings = MongoClientSettings.FromConnectionString(connectionString);
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(2);
        settings.ConnectTimeout = TimeSpan.FromSeconds(2);

        // A unique cluster key keeps registry cleanup from disposing another client's shared cluster.
        settings.ApplicationName = $"chronicle-primary-readiness-{Guid.NewGuid():N}";
        return new MongoClient(settings);
    }

    internal static void DisposeReadinessClient(MongoClient client)
    {
        // In MongoDB.Driver 3.12, MongoClient.Dispose does not unregister the default cluster.
        var cluster = client.Cluster;
        client.Dispose();
        ClusterRegistry.Instance.UnregisterAndDisposeCluster(cluster);
    }

    internal async Task WaitForPrimary(Func<CancellationToken, Task<BsonDocument>> probe)
    {
        // Testcontainers 4.15.0 caches IContainer.State at its last readiness inspection; it cannot
        // detect a subsequent exit here. Retain the 45-second deadline rather than polling Docker.
        using var timeout = new CancellationTokenSource(_primaryReadinessTimeout);
        BsonDocument? lastHello = null;
        Exception? lastError = null;

        try
        {
            while (true)
            {
                try
                {
                    lastHello = await probe(timeout.Token);
                    if (IsReady(lastHello))
                    {
                        return;
                    }
                }
                catch (Exception error) when (error is MongoException or TimeoutException)
                {
                    // Connecting and electing a primary can fail transiently during container startup.
                    lastError = error;
                }

                // Infrastructure readiness backoff, bounded by the same cancellation deadline.
                await Task.Delay(100, timeout.Token);
            }
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"MongoDB replica set rs0 did not elect a usable PRIMARY at {ConnectionString} within {_primaryReadinessTimeout.TotalSeconds} seconds. " +
                $"Last hello: {lastHello?.ToJson() ?? "<none>"}. Last MongoDB error: {lastError?.Message ?? "<none>"}.");
        }
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}
