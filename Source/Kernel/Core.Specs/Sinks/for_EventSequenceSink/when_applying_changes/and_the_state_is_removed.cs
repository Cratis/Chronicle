// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Sinks.for_EventSequenceSink.when_applying_changes;

public class and_the_state_is_removed : given.a_sink
{
    IEnumerable<Storage.Sinks.FailedPartition> _failures;

    async Task Because()
    {
        var changeset = Fold(new(), 10, 1, 5);
        changeset.Remove();
        _failures = await _sink.ApplyChanges("customer-1", changeset, 5);
    }

    [Fact] void should_fail_explicitly() => _failures.Count().ShouldEqual(1);
    [Fact] void should_not_publish() => _destinations.Events(Store, Tenant, EventSequenceId.Outbox).ShouldBeEmpty();
}
