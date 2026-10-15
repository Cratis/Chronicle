// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_SequencingKeyHash;

public class when_restoring_an_invalid_length : Specification
{
    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    [InlineData(33)]
    public void should_reject_non_sha256_lengths(int length) => Catch.Exception(() => SequencingKeyHash.FromStored(new byte[length])).ShouldBeOfExactType<InvalidCanonicalSequencingKey>();
}
