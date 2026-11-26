// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_MongoDBCapabilityProbe;

public class when_commands_succeed : Specification
{
    MongoDBCapabilityProbe _probe;
    MongoDBCapabilityReport _report;
    int _changeStreams;
    int _collation;

    void Establish() => _probe = new(
        _ =>
        {
            _changeStreams++;
            return Task.CompletedTask;
        },
        _ =>
        {
            _collation++;
            return Task.CompletedTask;
        });

    async Task Because() => _report = await _probe.Probe(CancellationToken.None);

    [Fact] void should_support_change_streams() => _report.ChangeStreams.ShouldEqual(MongoDBCapability.Supported);
    [Fact] void should_support_collation() => _report.Collation.ShouldEqual(MongoDBCapability.Supported);
    [Fact] void should_probe_change_streams_once() => _changeStreams.ShouldEqual(1);
    [Fact] void should_probe_collation_once() => _collation.ShouldEqual(1);
}
