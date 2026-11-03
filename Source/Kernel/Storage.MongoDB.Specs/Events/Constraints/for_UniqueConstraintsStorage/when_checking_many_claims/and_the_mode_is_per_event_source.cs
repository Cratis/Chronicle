// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_UniqueConstraintsStorage.when_checking_many_claims;

[Collection(MongoDBCollection.Name)]
public class and_the_mode_is_per_event_source(MongoDBFixture fixture) : given.a_unique_constraints_storage(fixture)
{
    UniqueConstraintClaim[] _claims;
    IReadOnlyList<(bool IsAllowed, EventSequenceNumber SequenceNumber)> _many;
    List<(bool IsAllowed, EventSequenceNumber SequenceNumber)> _single;

    async Task Establish()
    {
        await _storage.Save("holder", _definition, 7, "taken", "scope");
        await _storage.Save("elsewhere", _definition, 8, "free", "other-scope");
        _claims =
        [
            new("holder", "taken"),
            new("newcomer", "taken"),
            new("newcomer", "free"),
            new("another", "taken")
        ];
    }

    async Task Because()
    {
        _many = await _storage.IsAllowedMany(_claims, _definition, "scope");
        _single = [];
        foreach (var claim in _claims)
        {
            _single.Add(await _storage.IsAllowed(claim.EventSourceId, _definition, claim.Value, "scope"));
        }
    }

    [Fact] void should_answer_every_claim_as_checking_it_on_its_own_does() => _many.ShouldContainOnly(_single);
    [Fact] void should_answer_in_the_order_of_the_claims() => _many.Select(_ => _.IsAllowed).ShouldEqual(_single.Select(_ => _.IsAllowed));
    [Fact] void should_allow_the_holder() => _many[0].ShouldEqual((true, (EventSequenceNumber)7UL));
    [Fact] void should_refuse_another_event_source_with_the_holding_sequence_number() => _many[1].ShouldEqual((false, (EventSequenceNumber)7UL));
    [Fact] void should_allow_a_value_held_only_in_another_scope() => _many[2].ShouldEqual((true, EventSequenceNumber.Unavailable));
}
