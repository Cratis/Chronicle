// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Receivers.for_ProvisionalDeliveryStatusCodes;

public class when_mapping_outcomes : Specification
{
    readonly ProvisionalDeliveryStatusCodes _codes = new();
    [Fact] void should_map_success() => _codes.For(DeliveryOutcome.Rejected(DeliveryStatus.Succeeded, string.Empty)).ShouldEqual(200);
    [Fact] void should_map_failure() => _codes.For(DeliveryOutcome.Rejected(DeliveryStatus.Failed, string.Empty)).ShouldEqual(500);
    [Fact] void should_map_cancellation() => _codes.For(DeliveryOutcome.Rejected(DeliveryStatus.Cancelled, string.Empty)).ShouldEqual(503);
    [Fact] void should_map_unknown_observers() => _codes.For(DeliveryOutcome.Rejected(DeliveryStatus.UnknownObserver, string.Empty)).ShouldEqual(404);
    [Fact] void should_map_malformed_deliveries() => _codes.For(DeliveryOutcome.Rejected(DeliveryStatus.Malformed, string.Empty)).ShouldEqual(400);
    [Fact] void should_map_oversized_deliveries() => _codes.For(DeliveryOutcome.Rejected(DeliveryStatus.TooLarge, string.Empty)).ShouldEqual(413);
}
