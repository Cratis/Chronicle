// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.Delivery.for_DeliveryRanges.when_checking_bounded_values;

public class and_values_are_at_the_maximum : Specification
{
    [Theory]
    [InlineData(2)]
    [InlineData(1023)]
    [InlineData(8192)]
    [InlineData(64)]
    [InlineData(65536)]
    [InlineData(int.MaxValue)]
    void should_preserve_bounded_int32_maxima(int maximum) => DeliveryRanges.Bounded(maximum, "field", 0, maximum).ShouldEqual(maximum);

    [Fact] void should_convert_the_int64_maximum_without_wrapping() => DeliveryRanges.Counter(long.MaxValue, "field").ShouldEqual(long.MaxValue);
    [Fact] void should_restore_the_int64_maximum_without_wrapping() => DeliveryRanges.ReadInt64(long.MaxValue, "field").ShouldEqual((ulong)long.MaxValue);
    [Fact] void should_restore_the_maximum_clock() => DeliveryRanges.ReadInt64(DeliveryRanges.MaximumClock, "clock", 0, DeliveryRanges.MaximumClock).ShouldEqual((ulong)DeliveryRanges.MaximumClock);
}
