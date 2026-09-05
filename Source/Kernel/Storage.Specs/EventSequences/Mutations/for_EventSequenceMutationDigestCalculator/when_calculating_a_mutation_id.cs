// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationDigestCalculator;

public class when_calculating_a_mutation_id : Specification
{
    EventSequenceMutationId _result;
    EventSequenceMutationId[] _changed;

    void Because()
    {
        var target = EventSequenceMutationIdentity.TryCreate("event-log").Identity!;
        var origin = EventSequenceMutationIdentity.TryCreate("system").Identity!;
        _result = EventSequenceMutationDigestCalculator.CalculateId(target, origin, 42UL, EventSequenceMutationKind.Revision);
        _changed =
        [
            EventSequenceMutationDigestCalculator.CalculateId(EventSequenceMutationIdentity.TryCreate("other").Identity!, origin, 42UL, EventSequenceMutationKind.Revision),
            EventSequenceMutationDigestCalculator.CalculateId(target, EventSequenceMutationIdentity.TryCreate("other").Identity!, 42UL, EventSequenceMutationKind.Revision),
            EventSequenceMutationDigestCalculator.CalculateId(target, origin, 43UL, EventSequenceMutationKind.Revision),
            EventSequenceMutationDigestCalculator.CalculateId(target, origin, 42UL, EventSequenceMutationKind.PointRedaction)
        ];
    }

    [Fact] void should_match_the_pinned_network_order_identifier() => _result.ShouldEqual((EventSequenceMutationId)Guid.Parse("369a7ea7-d3e8-8fc7-8045-6ad4d9c7e409"));
    [Fact] void should_use_the_reserved_version_eight_nibble() => ((_result.Value.ToByteArray(bigEndian: true)[6] & 0xf0) == 0x80).ShouldBeTrue();
    [Fact] void should_use_the_rfc_variant() => ((_result.Value.ToByteArray(bigEndian: true)[8] & 0xc0) == 0x80).ShouldBeTrue();
    [Fact] void should_bind_every_identity_input() => _changed.All(_ => _ != _result).ShouldBeTrue();
}
