// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Sequencing;

namespace Cratis.Chronicle.Storage.Observation.Delivery.for_SliceRange.when_testing_membership;

public class and_the_end_is_exclusive : Specification
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    void should_apply_half_open_semantics(ushort slice, bool expected) => new SliceRange(1, 3).Contains(new SequencingKeySlice(slice)).ShouldEqual(expected);

    [Fact] void should_include_the_final_slice_in_all() => SliceRange.All.Contains(new SequencingKeySlice(1023)).ShouldBeTrue();
}
