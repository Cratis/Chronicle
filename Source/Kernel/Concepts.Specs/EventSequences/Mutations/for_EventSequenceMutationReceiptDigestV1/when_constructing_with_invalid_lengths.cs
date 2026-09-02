// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.EventSequences.Mutations.for_EventSequenceMutationReceiptDigestV1;

public class when_constructing_with_invalid_lengths : Specification
{
    EventSequenceMutationReceiptDigestV1 _digest;
    Exception[] _errors;

    void Because() => _errors =
    [
        Catch.Exception(() => _digest = new(new byte[31])),
        Catch.Exception(() => _digest = new(new byte[33]))
    ];

    [Fact] void should_reject_every_invalid_length_with_a_typed_failure() => _errors.All(_ => _ is InvalidEventSequenceMutationDigestLength).ShouldBeTrue();
}
