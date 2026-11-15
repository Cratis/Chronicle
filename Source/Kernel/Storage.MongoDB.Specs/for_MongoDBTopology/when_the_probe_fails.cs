// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.for_MongoDBTopology.given;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBTopology;

public class when_the_probe_fails : a_topology
{
    bool _whileUnreachable;
    int _probesBeforeRetry;
    bool _afterRetry;

    void Establish() => _reply = () => throw new TimeoutException("unreachable");

    async Task Because()
    {
        Start();
        await _topology.Probing;
        _whileUnreachable = _topology.IsStandalone;
        await _topology.Probing;
        _probesBeforeRetry = _probes;
        _reply = () => new("isWritablePrimary", true);
        _time.Advance(MongoDBTopology.RetryInterval);
        _ = _topology.IsStandalone;
        await _topology.Probing;
        _afterRetry = _topology.IsStandalone;
    }

    [Fact] void should_not_assume_a_weaker_topology() => _whileUnreachable.ShouldBeFalse();
    [Fact] void should_not_probe_again_before_the_retry_interval() => _probesBeforeRetry.ShouldEqual(1);
    [Fact] void should_classify_the_server_once_it_answers() => _afterRetry.ShouldBeTrue();
}
