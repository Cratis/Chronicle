// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationDigestCalculator;

public class when_given_malformed_utf16_identifiers : given.a_digest_calculation
{
    Exception[] _identityErrors;
    Exception[] _frameTextErrors;

    void Because()
    {
        _identityErrors =
        [
            Catch.Exception(() => CalculateDefinition(scope: _scope with { EventSequenceId = "\ud800" })),
            Catch.Exception(() => CalculateDefinition(mutation: _mutation with { Origin = _mutation.Origin with { Sequence = "\udc00" } }))
        ];
        _frameTextErrors =
        [
            Catch.Exception(() => CalculateDefinition(scope: _scope with { EventStore = "\ud800" })),
            Catch.Exception(() => CalculateDefinition(scope: _scope with { Namespace = "\udc00" })),
            Catch.Exception(() => CalculateDefinition(mutation: _mutation with { Command = _mutation.Command with { Payload = "\ud800" } })),
            Catch.Exception(() => CalculateDefinition(mutation: _mutation with { Command = _mutation.Command with { Hash = "\udc00" } }))
        ];
    }

    [Fact] void should_reject_both_target_and_origin_with_a_typed_failure() => _identityErrors.All(_ => _ is UnsupportedEventSequenceId { Reason: UnsupportedEventSequenceIdReason.IllFormedUtf16 }).ShouldBeTrue();
    [Fact] void should_reject_every_other_framed_text_field_with_a_typed_failure() => _frameTextErrors.All(_ => _ is InvalidEventSequenceMutationFrameText).ShouldBeTrue();
}
