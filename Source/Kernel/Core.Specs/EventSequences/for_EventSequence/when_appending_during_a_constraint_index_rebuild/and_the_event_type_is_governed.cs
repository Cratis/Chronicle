// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_during_a_constraint_index_rebuild;

public class and_the_event_type_is_governed : given.a_rebuilding_constraint_index
{
    Task Establish() => _eventSequence.ConstraintIndexRebuildStarting(_runId, [_uniqueDefinition.Name]);

    async Task Because() => _result = await _eventSequence.Append(
        EventSourceType.Default,
        _eventSourceId,
        EventStreamType.All,
        EventStreamId.Default,
        _eventType,
        new(),
        CorrelationId.New(),
        [],
        Identity.System,
        [],
        ConcurrencyScope.None);

    [Fact] void should_refuse_with_the_retryable_index_error() => _result.Errors.ShouldContainOnly(AppendError.ConstraintIndexUnavailable);
    [Fact] void should_not_report_a_business_constraint_violation() => _result.HasConstraintViolations.ShouldBeFalse();
    [Fact] void should_not_store_the_event() => _log.Events.Count.ShouldEqual(2);
    [Fact] void should_not_change_the_index() => _claimWrites.ShouldEqual(0);
}
