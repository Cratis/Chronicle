// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.Delivery.for_SliceRange.when_constructing_a_range;

public class and_the_range_is_invalid : Specification
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(2, 1)]
    [InlineData(0, 1025)]
    void should_reject_the_interval(ushort from, ushort to) =>
        Catch.Exception(() => _ = new SliceRange(from, to)).ShouldBeOfExactType<DeliveryValueOutOfRange>();
}
