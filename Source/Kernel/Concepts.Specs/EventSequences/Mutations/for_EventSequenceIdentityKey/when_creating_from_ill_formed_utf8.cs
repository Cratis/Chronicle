// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.EventSequences.Mutations.for_EventSequenceIdentityKey;

public class when_creating_from_ill_formed_utf8 : Specification
{
    EventSequenceIdentityKey _key;
    Exception _error;

    void Because() => _error = Catch.Exception(() => _key = new([0xc3, 0x28]));

    [Fact] void should_reject_the_bytes_with_a_typed_failure() => _error.ShouldBeOfExactType<InvalidEventSequenceIdentityKey>();
}
