// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Schema.for_MongoDBCapabilityProbe;

public class when_the_probe_deadline_expires : Specification
{
    TaskCompletionSource _pending;
    MongoDBCapabilityProbe _probe;
    MongoDBCapabilityReport _report;

    void Establish()
    {
        _pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _probe = new(_ => _pending.Task, _ => Task.CompletedTask, TimeSpan.FromMilliseconds(1));
    }

    async Task Because() => _report = await _probe.Probe(CancellationToken.None);

    [Fact] void should_not_report_unsupported() => _report.Failures.Any().ShouldBeFalse();
    [Fact] void should_report_the_timeout_as_inconclusive() => _report.ChangeStreams.State.ShouldEqual(MongoDBCapabilityState.Inconclusive);
    [Fact] void should_still_probe_collation() => _report.Collation.ShouldEqual(MongoDBCapability.Supported);

    void Destroy() => _pending.TrySetResult();
}
