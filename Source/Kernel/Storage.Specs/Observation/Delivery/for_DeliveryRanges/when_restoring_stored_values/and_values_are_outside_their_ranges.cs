// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.Delivery.for_DeliveryRanges.when_restoring_stored_values;

public class and_values_are_outside_their_ranges : Specification
{
    [Theory]
    [InlineData(-1, 0, long.MaxValue)]
    [InlineData(0, 1, long.MaxValue)]
    [InlineData(3155378976000000000, 0, DeliveryRanges.MaximumClock)]
    void should_fail_closed_for_int64(long value, long minimum, long maximum) =>
        Catch.Exception(() => DeliveryRanges.ReadInt64(value, "field", minimum, maximum)).ShouldBeOfExactType<CorruptDeliveryRow>();

    [Theory]
    [InlineData(-1, 0, int.MaxValue)]
    [InlineData(0, 1, int.MaxValue)]
    [InlineData(3, 0, 2)]
    [InlineData(1024, 0, 1023)]
    [InlineData(65, 1, 64)]
    [InlineData(8193, 1, 8192)]
    [InlineData(65537, 0, 65536)]
    void should_fail_closed_for_int32(int value, int minimum, int maximum) =>
        Catch.Exception(() => DeliveryRanges.ReadInt32(value, "field", minimum, maximum)).ShouldBeOfExactType<CorruptDeliveryRow>();
}
