// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;

namespace Cratis.Chronicle.Events.Constraints.for_ReindexConstraintsStep.when_performing;

public class and_a_governed_type_has_no_schema : for_ReindexConstraintsStep.given.a_handoff_reindex_step
{
    void Establish() => _eventTypes.GetFor((EventTypeId)"Registered", Arg.Any<EventTypeGeneration?>()).Returns(Task.FromException<EventTypeSchema>(new MissingEventSchemaForEventType("store", "Registered", EventTypeGeneration.First)));

    async Task Because() => await Perform();

    [Fact] void should_fail_with_the_missing_schema()
    {
        _result.TryGetException(out var error).ShouldBeTrue();
        error.ShouldBeOfExactType<MissingEventSchemaForEventType>();
    }
    [Fact] async Task should_report_unknown_progress() => await _sequence.Received(1).ConstraintIndexRebuilt(_runId, Arg.Any<IEnumerable<Concepts.Events.Constraints.ConstraintName>>(), ConstraintIndexRebuildOutcome.ProgressUnknown);
}
