// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_UniqueConstraintsStorage.when_checking_many_claims;

[Collection(MongoDBCollection.Name)]
public class and_the_mode_is_per_value(MongoDBFixture fixture) : given.a_unique_constraints_storage(fixture)
{
    UniqueConstraintClaim[] _claims;
    IReadOnlyList<(bool IsAllowed, EventSequenceNumber SequenceNumber)> _many;
    List<(bool IsAllowed, EventSequenceNumber SequenceNumber)> _single;

    async Task Establish()
    {
        _definition = _definition with { Mode = UniqueConstraintMode.PerValue };
        await _storage.Save("holder", _definition, 3, "first", "scope");
        await _storage.Save("holder", _definition, 4, "second", "scope");
        _claims =
        [
            new("holder", "first"),
            new("holder", "second"),
            new("newcomer", "second"),
            new("newcomer", "third")
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

    [Fact] void should_answer_every_claim_as_checking_it_on_its_own_does() => _many.ShouldEqual(_single);
    [Fact] void should_refuse_a_retained_value_for_another_event_source() => _many[2].ShouldEqual((false, (EventSequenceNumber)4UL));
    [Fact] void should_allow_a_value_nobody_holds() => _many[3].ShouldEqual((true, EventSequenceNumber.Unavailable));
}
