// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_activating.and_no_watermark_is_persisted;

public class and_a_value_moved_between_sources_before_the_boundary : given.a_catching_up_sequence
{
    bool _markerWasPersistedBeforeRelease;

    async Task Establish()
    {
        await TransferHistory();
        _unique.Remove(Arg.Any<EventSourceId>(), Arg.Any<UniqueConstraintDefinition>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                _markerWasPersistedBeforeRelease = _snapshots.Written!.ConstraintIndexCatchUpRefusing?.Contains(_uniqueDefinition.Name) == true;
                await _claims.Remove(call.ArgAt<EventSourceId>(0), call.ArgAt<UniqueConstraintDefinition>(1), call.ArgAt<string>(2));
                _clock.Advance(TimeSpan.FromSeconds(2));
            });
    }

    async Task Because()
    {
        await Tick();
        _expandoObjectConverter.ToExpandoObject(Arg.Any<System.Text.Json.Nodes.JsonObject>(), Arg.Any<Schemas.JsonSchema>()).Returns(Content("value"));
        _result = await AppendAnEvent();
    }

    [Fact] async Task should_finish_the_claim_in_the_release_turn() => (await Values("new-holder")).ShouldContain(Hash("value"));
    [Fact] void should_persist_refusal_before_releasing() => _markerWasPersistedBeforeRelease.ShouldBeTrue();
    [Fact] void should_never_admit_a_duplicate_between_turns() => _result.ConstraintViolations.ShouldNotBeEmpty();
}
