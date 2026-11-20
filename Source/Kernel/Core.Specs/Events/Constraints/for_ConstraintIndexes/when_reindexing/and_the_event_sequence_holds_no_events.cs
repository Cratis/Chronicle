// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexes.when_reindexing;

public class and_the_event_sequence_holds_no_events : given.an_event_store_with_a_log_and_an_outbox
{
    void Establish() => _logStorage.GetTailSequenceNumber().Returns(EventSequenceNumber.Unavailable);

    async Task Because()
    {
        _calls.Clear();
        await _constraintIndexes.Reindex(_eventStore, _namespace, EventSequenceId.Log, []);
    }

    [Fact] void should_start_the_explicitly_requested_reindex() => _startedReindexes.Single().EventSequenceId.ShouldEqual(EventSequenceId.Log);
    [Fact] void should_refresh_and_start_without_checking_the_tail() => _calls.ShouldEqual(new[] { $"refresh {EventSequenceId.Log}", $"reindex {EventSequenceId.Log}" });
}
