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
            CalculateDefinition(mutation: WithRequest(_mutation.Definition.Request with { TargetSequence = EventSequenceMutationIdentity.TryCreate("another-target").Identity! })),
            CalculateDefinition(mutation: WithRequest(_mutation.Definition.Request with { Id = Guid.Parse("10112233-4455-6677-8899-aabbccddeeff") })),
            CalculateDefinition(mutation: WithRequest(_mutation.Definition.Request with { Origin = _mutation.Origin with { Sequence = EventSequenceMutationIdentity.TryCreate("another-origin").Identity! } })),
            CalculateDefinition(mutation: WithRequest(_mutation.Definition.Request with { Origin = _mutation.Origin with { SequenceNumber = 2UL } })),
            CalculateDefinition(mutation: WithRequest(_mutation.Definition.Request with { Kind = EventSequenceMutationKind.PointRedaction })),
            CalculateDefinition(mutation: WithRequest(_mutation.Definition.Request with { Command = _mutation.Command with { Payload = "{\"name\":\"Grace\"}" } })),
            CalculateDefinition(mutation: WithRequest(_mutation.Definition.Request with { Command = _mutation.Command with { Hash = "another-hash" } })),
            CalculateDefinition(mutation: _mutation with { Definition = _mutation.Definition with { Target = _mutation.Target with { Start = 9UL } } }),
            CalculateDefinition(mutation: _mutation with { Definition = _mutation.Definition with { Target = _mutation.Target with { EndExclusive = 14UL } } }),
            CalculateDefinition(mutation: _mutation with { Definition = _mutation.Definition with { Target = _mutation.Target with { ExpectedCount = 4UL } } })
        ];
    }

    [Fact] void should_make_every_framed_field_affect_the_digest() => _changed.All(_ => _ != _baseline).ShouldBeTrue();
    [Fact] void should_cover_every_definition_field() => _changed.Length.ShouldEqual(12);

    EventSequenceMutation WithRequest(EventSequenceMutationRequest request) =>
        _mutation with { Definition = _mutation.Definition with { Request = request } };
}
