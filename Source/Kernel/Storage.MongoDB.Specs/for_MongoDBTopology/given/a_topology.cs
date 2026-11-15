// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBTopology.given;

public class a_topology : Specification
{
    protected MongoDBTopology _topology;
    protected ManualTime _time;
    protected int _probes;
    protected Func<BsonDocument?> _reply = () => new BsonDocument("isWritablePrimary", true);

    void Establish() => _time = new();

    /// <summary>
    /// Create the topology, which starts probing at once; call it only after the reply is arranged.
    /// </summary>
    protected void Start() =>
        _topology = new(
            _ =>
            {
                Interlocked.Increment(ref _probes);
                return Task.FromResult(_reply());
            },
            NullLogger.Instance,
            _time);

    protected class ManualTime : TimeProvider
    {
        DateTimeOffset _now = DateTimeOffset.UnixEpoch;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan duration) => _now += duration;
    }
}
