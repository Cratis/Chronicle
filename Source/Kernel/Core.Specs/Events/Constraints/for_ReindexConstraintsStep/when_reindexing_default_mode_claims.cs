// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ReindexConstraintsStep;

public class when_reindexing_default_mode_claims : given.a_unique_constraint_to_reindex
{
    EventSourceId _firstOwner;
    EventSourceId _laterOwner;
    Storage.InMemory.Events.Constraints.UniqueConstraintsStorage _claims;
    UniqueConstraintValue _staleValue;
    bool _staleClaimWasClearedBeforeReplay;

    async Task Establish()
    {
        _firstOwner = EventSourceId.New();
        _laterOwner = EventSourceId.New();
        _claims = new();
        _staleValue = new List<UniqueConstraintPropertyAndValue> { new(Property, "first") }.GetValue();
        await _claims.Save(_laterOwner, _definition, 42UL, _staleValue);
        _storage.ClearValues(_definition, Arg.Any<string>()).Returns(call => _claims.ClearValues(_definition, call.Arg<string>()));
        _storage.Save(Arg.Any<EventSourceId>(), _definition, Arg.Any<EventSequenceNumber>(), Arg.Any<UniqueConstraintValue>(), Arg.Any<string>()).Returns(async call =>
        {
            if (call.Arg<EventSourceId>() == _firstOwner)
            {
                _staleClaimWasClearedBeforeReplay = (await _claims.IsAllowed(_firstOwner, _definition, _staleValue)).IsAllowed;
            }

            await _claims.Save(call.Arg<EventSourceId>(), _definition, call.Arg<EventSequenceNumber>(), call.Arg<UniqueConstraintValue>(), call.Arg<string>());
        });
    }

    async Task Because()
    {
        await ReindexConstraintsStep.ReindexEvent(_definition, EventFor(_firstOwner), ContentWith("first"), _seen, _validator, _storage);
        await ReindexConstraintsStep.ReindexEvent(_definition, EventFor(_laterOwner), ContentWith("later"), _seen, _validator, _storage);
    }

    [Fact] void should_clear_stale_claims_before_the_first_event_is_replayed() => _staleClaimWasClearedBeforeReplay.ShouldBeTrue();
    [Fact] async Task should_clear_the_whole_scope_only_once() => await _storage.Received(1).ClearValues(_definition, "");
    [Fact] async Task should_not_clear_individual_owners() => await _storage.DidNotReceive().Remove(Arg.Any<EventSourceId>(), _definition, Arg.Any<string>());
    [Fact] void should_track_one_cleared_scope() => _seen.Count.ShouldEqual(1);
}
