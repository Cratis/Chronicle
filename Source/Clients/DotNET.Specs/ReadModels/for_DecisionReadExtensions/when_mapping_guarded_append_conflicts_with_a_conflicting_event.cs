// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.ReadModels.for_DecisionReadExtensions;

public class when_mapping_guarded_append_conflicts_with_a_conflicting_event : Specification
{
    DecisionConflict _conflict;

    void Because()
    {
        var violation = new ConcurrencyViolation("source", 24, 27) { ConflictingEventTypeId = "created", ConflictingEventSequenceNumber = 25 };
        var result = new AppendManyResult { ConcurrencyViolations = [violation] };
        IDecisionRead[] reads = [new DecisionRead<object>("source", null, "store", "namespace", 24, [new EventType("created", 1)])];
        _conflict = result.GetDecisionConflicts(reads).Single();
    }

    [Fact] void should_name_the_conflicting_event_type() => _conflict.ConflictingEventTypeId.ShouldEqual(new EventTypeId("created"));
    [Fact] void should_name_the_conflicting_event_sequence_number() => _conflict.ConflictingEventSequenceNumber.ShouldEqual(new EventSequenceNumber(25));
}
