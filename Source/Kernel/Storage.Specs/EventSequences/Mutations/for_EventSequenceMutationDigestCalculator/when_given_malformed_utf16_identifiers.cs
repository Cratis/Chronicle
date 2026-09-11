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
            Catch.Exception(() => CalculateReceipt(scope: _scope with { EventSequenceId = "\ud800" }))
        ];
        _frameTextErrors =
        [
            Catch.Exception(() => CalculateDefinition(scope: _scope with { EventStore = "\ud800" })),
            Catch.Exception(() => CalculateDefinition(scope: _scope with { Namespace = "\udc00" })),
            Catch.Exception(() => CalculateDefinition(mutation: WithCommand(_mutation.Command with { Payload = "\ud800" }))),
            Catch.Exception(() => CalculateDefinition(mutation: WithCommand(_mutation.Command with { Hash = "\udc00" })))
        ];
    }

    [Fact] void should_reject_the_scope_identity_with_a_typed_failure() => _identityErrors.All(_ => _ is UnsupportedEventSequenceId { Reason: UnsupportedEventSequenceIdReason.IllFormedUtf16 }).ShouldBeTrue();
    [Fact] void should_reject_every_other_framed_text_field_with_a_typed_failure() => _frameTextErrors.All(_ => _ is InvalidEventSequenceMutationFrameText).ShouldBeTrue();

    EventSequenceMutation WithCommand(EventSequenceMutationCommandEnvelope command) =>
        _mutation with
        {
            Definition = _mutation.Definition with
            {
                Request = _mutation.Definition.Request with { Command = command }
            }
        };
}
