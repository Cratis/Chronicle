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
/// A probe that fails is not cached; it is retried after <see cref="RetryInterval"/> so a server that is briefly
/// unreachable at startup is still classified, without every caller paying for a probe while it is down.
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

    readonly Func<CancellationToken, BsonDocument?> _hello;
    readonly ILogger _logger;
    readonly TimeProvider _timeProvider;
    readonly Lock _lock = new();
    bool? _standalone;
    DateTimeOffset _nextProbe = DateTimeOffset.MinValue;

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
    internal MongoDBTopology(Func<CancellationToken, BsonDocument?> hello, ILogger logger, TimeProvider timeProvider)
    {
        _hello = hello;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc/>
    public bool IsStandalone => _standalone ?? Probe();

    /// <summary>
    /// Classify a reply to the hello command.
    /// </summary>
    /// <param name="hello">The reply.</param>
    /// <returns><see langword="true"/> when the reply is from a standalone MongoDB server.</returns>
    internal static bool IsStandaloneReply(BsonDocument hello) =>
        !hello.Contains("setName") &&
        !(hello.TryGetValue("isreplicaset", out var replicaSet) && replicaSet.ToBoolean()) &&
        !(hello.TryGetValue("msg", out var message) && message.IsString && message.AsString == "isdbgrid");

    static BsonDocument? Hello(IDatabase database, CancellationToken cancellationToken) =>
        database.GetCollection<BsonDocument>(nameof(Hello)).Database?
            .RunCommand<BsonDocument>(new BsonDocument("hello", 1), cancellationToken: cancellationToken);

    bool Probe()
    {
        lock (_lock)
        {
            if (_standalone is { } known) return known;
            if (_timeProvider.GetUtcNow() < _nextProbe) return false;
            _nextProbe = _timeProvider.GetUtcNow() + RetryInterval;
            try
            {
                using var timeout = new CancellationTokenSource(ProbeTimeout, _timeProvider);
                if (_hello(timeout.Token) is not { } reply) return false;
                var standalone = IsStandaloneReply(reply);
                if (standalone) _logger.StandaloneServer();
                _standalone = standalone;
                return standalone;
            }
            catch (Exception exception) when (exception is MongoException or TimeoutException or OperationCanceledException)
            {
                _logger.TopologyUnknown(exception);
                return false;
            }
        }
    }
}
