// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_SequencingKeySlice;

public class when_constructing_out_of_range : Specification
{
    [Theory]
    [InlineData((ushort)1024)]
    [InlineData(ushort.MaxValue)]
    public void should_reject_invalid_slices(ushort value) => Catch.Exception(() => _ = new SequencingKeySlice(value)).ShouldBeOfExactType<InvalidSequencingKey>();
}
