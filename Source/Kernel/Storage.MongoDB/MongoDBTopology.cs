// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IMongoDBTopology"/> that asks the server with the hello command once and caches the answer.
/// </summary>
/// <remarks>
/// The probe starts when the topology is created and runs in the background, so reading <see cref="IsStandalone"/> never
/// waits on the server. A probe that fails is not cached; it is started again on a read after <see cref="RetryInterval"/>.
/// </remarks>
public class MongoDBTopology : IMongoDBTopology
{
    /// <summary>
    /// The longest a single probe may take, including server selection.
    /// </summary>
    internal static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(2);

    /// <summary>
    /// The pause before a failed probe is tried again.
    /// </summary>
    internal static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(30);

    const int Unknown = 0;
    const int Replicated = 1;
    const int Standalone = 2;

    readonly Func<CancellationToken, Task<BsonDocument?>> _hello;
    readonly ILogger _logger;
    readonly TimeProvider _timeProvider;
    readonly Lock _lock = new();
    volatile int _state = Unknown;
    DateTimeOffset _nextProbe = DateTimeOffset.MinValue;
    Task _probe = Task.CompletedTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBTopology"/> class.
    /// </summary>
    /// <param name="database">The <see cref="IDatabase"/> whose server is probed.</param>
    /// <param name="logger">Optional <see cref="ILogger{TCategoryName}"/> the standalone warning is written to.</param>
    public MongoDBTopology(IDatabase database, ILogger<MongoDBTopology>? logger = null)
        : this(cancellationToken => Hello(database, cancellationToken), logger ?? NullLogger<MongoDBTopology>.Instance, TimeProvider.System)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBTopology"/> class with an explicit probe.
    /// </summary>
    /// <param name="hello">Runs the hello command against the server.</param>
    /// <param name="logger">The <see cref="ILogger"/> the standalone warning is written to.</param>
    /// <param name="timeProvider">The <see cref="TimeProvider"/> the retry interval is measured with.</param>
    internal MongoDBTopology(Func<CancellationToken, Task<BsonDocument?>> hello, ILogger logger, TimeProvider timeProvider)
    {
        _hello = hello;
        _logger = logger;
        _timeProvider = timeProvider;
        StartProbeIfDue();
    }

    /// <inheritdoc/>
    public bool IsStandalone
    {
        get
        {
            if (_state == Unknown) StartProbeIfDue();
            return _state == Standalone;
        }
    }

    /// <summary>
    /// Gets the probe in flight, or the last one to finish.
    /// </summary>
    internal Task Probing
    {
        get
        {
            lock (_lock)
            {
                return _probe;
            }
        }
    }

    /// <summary>
    /// Classify a reply to the hello command.
    /// </summary>
    /// <param name="hello">The reply.</param>
    /// <returns><see langword="true"/> when the reply is from a standalone MongoDB server.</returns>
    internal static bool IsStandaloneReply(BsonDocument hello) =>
        !hello.Contains("setName") &&
        !(hello.TryGetValue("isreplicaset", out var replicaSet) && replicaSet.ToBoolean()) &&
        !(hello.TryGetValue("msg", out var message) && message.IsString && message.AsString == "isdbgrid");

    static async Task<BsonDocument?> Hello(IDatabase database, CancellationToken cancellationToken) =>
        database.GetCollection<BsonDocument>(nameof(Hello)).Database is { } server
            ? await server.RunCommandAsync<BsonDocument>(new BsonDocument("hello", 1), cancellationToken: cancellationToken)
            : null;

    void StartProbeIfDue()
    {
        lock (_lock)
        {
            if (_state != Unknown || !_probe.IsCompleted || _timeProvider.GetUtcNow() < _nextProbe) return;
            _nextProbe = _timeProvider.GetUtcNow() + RetryInterval;
            _probe = Task.Run(Probe);
        }
    }

    async Task Probe()
    {
        try
        {
            using var timeout = new CancellationTokenSource(ProbeTimeout, _timeProvider);
            if (await _hello(timeout.Token).WaitAsync(timeout.Token) is not { } reply) return;
            var standalone = IsStandaloneReply(reply);
            if (standalone) _logger.StandaloneServer();
            _state = standalone ? Standalone : Replicated;
        }
        catch (Exception exception) when (exception is MongoException or TimeoutException or OperationCanceledException)
        {
            _logger.TopologyUnknown(exception.GetType().Name);
        }
    }
}
