// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.Delivery.for_DeliveryRanges.when_checking_bounded_values;

public class and_the_range_is_exceeded : Specification
{
    [Theory]
    [InlineData(3, 0, 2)]
    [InlineData(1024, 0, 1023)]
    [InlineData(0, 1, 8192)]
    [InlineData(8193, 1, 8192)]
    [InlineData(0, 1, 64)]
    [InlineData(65, 1, 64)]
    [InlineData(65537, 0, 65536)]
    void should_reject_key_kind_slice_key_size_lane_count_and_payload_size(int value, int minimum, int maximum) =>
        Catch.Exception(() => DeliveryRanges.Bounded(value, "field", minimum, maximum)).ShouldBeOfExactType<DeliveryValueOutOfRange>();
}
