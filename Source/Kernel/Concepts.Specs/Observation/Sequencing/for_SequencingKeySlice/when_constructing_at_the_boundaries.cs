// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_SequencingKeySlice;

public class when_constructing_at_the_boundaries : Specification
{
    [Theory]
    [InlineData((ushort)0)]
    [InlineData((ushort)1023)]
    public void should_accept_the_supported_boundaries(ushort value) => new SequencingKeySlice(value).Value.ShouldEqual(value);
}
