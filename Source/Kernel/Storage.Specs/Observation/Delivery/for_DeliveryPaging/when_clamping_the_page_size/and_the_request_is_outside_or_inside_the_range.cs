// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.Delivery.for_DeliveryPaging.when_clamping_the_page_size;

public class and_the_request_is_outside_or_inside_the_range : Specification
{
    [Theory]
    [InlineData(int.MinValue, 1)]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(50, 50)]
    [InlineData(1000, 1000)]
    [InlineData(1001, 1000)]
    [InlineData(int.MaxValue, 1000)]
    void should_clamp_to_the_supported_range(int requested, int expected) => DeliveryPaging.Clamp(requested).ShouldEqual(expected);
}
