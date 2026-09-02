// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationDigestCalculator;

public class when_changing_each_definition_field : given.a_digest_calculation
{
    EventSequenceMutationDefinitionDigestV1 _baseline;
    EventSequenceMutationDefinitionDigestV1[] _changed;

    void Because()
    {
        _baseline = CalculateDefinition();
        _changed =
        [
            CalculateDefinition(scope: _scope with { EventStore = "another-store" }),
            CalculateDefinition(scope: _scope with { Namespace = "another-namespace" }),
            CalculateDefinition(scope: _scope with { EventSequenceId = "another-target" }),
            CalculateDefinition(mutation: _mutation with { Id = Guid.Parse("10112233-4455-6677-8899-aabbccddeeff") }),
            CalculateDefinition(mutation: _mutation with { Origin = _mutation.Origin with { Sequence = "another-origin" } }),
            CalculateDefinition(mutation: _mutation with { Origin = _mutation.Origin with { SequenceNumber = 2UL } }),
            CalculateDefinition(mutation: _mutation with { Command = _mutation.Command with { Kind = EventSequenceMutationKind.PointRedaction } }),
            CalculateDefinition(mutation: _mutation with { Command = _mutation.Command with { Payload = "{\"name\":\"Grace\"}" } }),
            CalculateDefinition(mutation: _mutation with { Command = _mutation.Command with { Hash = "another-hash" } }),
            CalculateDefinition(mutation: _mutation with { Target = _mutation.Target with { Start = 9UL } }),
            CalculateDefinition(mutation: _mutation with { Target = _mutation.Target with { EndExclusive = 14UL } }),
            CalculateDefinition(mutation: _mutation with { Target = _mutation.Target with { ExpectedCount = 4UL } })
        ];
    }

    [Fact] void should_make_every_framed_field_affect_the_digest() => _changed.All(_ => _ != _baseline).ShouldBeTrue();
    [Fact] void should_cover_every_definition_field() => _changed.Length.ShouldEqual(12);
}
