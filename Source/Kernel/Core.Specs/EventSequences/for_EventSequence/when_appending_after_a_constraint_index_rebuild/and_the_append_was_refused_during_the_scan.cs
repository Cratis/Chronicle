// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_after_a_constraint_index_rebuild;

public class and_the_append_was_refused_during_the_scan : given.a_rebuilding_constraint_index
{
    AppendResult _duringRebuild;

    async Task Establish()
    {
        await _eventSequence.ConstraintIndexRebuildStarting(_runId, [_uniqueDefinition.Name]);
        _duringRebuild = await Append();
        await _claims.ClearValues(_uniqueDefinition);
        await Hold("source", "current");
    }

    async Task Because()
    {
        await _eventSequence.ConstraintIndexRebuilt(_runId, [_uniqueDefinition.Name], new(true, 1));
        _result = await Append();
    }

    Task<AppendResult> Append() => _eventSequence.Append(
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

    [Fact] void should_have_refused_the_append_during_the_scan() => _duringRebuild.Errors.ShouldContainOnly(AppendError.ConstraintIndexUnavailable);
    [Fact] void should_accept_the_same_append_after_handover() => _result.IsSuccess.ShouldBeTrue();
    [Fact] async Task should_index_the_accepted_value() => (await Values(_eventSourceId.Value)).ShouldContainOnly(Hash("claimed@example.com"));
    [Fact] async Task should_keep_the_historical_claim() => (await Values("source")).ShouldContainOnly(Hash("current"));
    [Fact] void should_store_only_the_accepted_append() => _log.Events.Count.ShouldEqual(3);
}
