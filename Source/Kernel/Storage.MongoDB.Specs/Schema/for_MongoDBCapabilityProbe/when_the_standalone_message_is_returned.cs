// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_MongoDBCapabilityProbe;

public class when_the_standalone_message_is_returned : Specification
{
    MongoDBCapabilityProbe _probe;
    MongoDBCapabilityReport _report;

    void Establish() => _probe = new(
        _ => Task.FromException(given.command_errors.Create(115, "The $changeStream stage is only supported on replica sets")),
        _ => Task.CompletedTask);

    async Task Because() => _report = await _probe.Probe(CancellationToken.None);

    [Fact] void should_be_inconclusive_despite_the_command_code() => _report.ChangeStreams.State.ShouldEqual(MongoDBCapabilityState.Inconclusive);
    [Fact] void should_not_have_unsupported_capabilities() => _report.Failures.Any().ShouldBeFalse();
}
