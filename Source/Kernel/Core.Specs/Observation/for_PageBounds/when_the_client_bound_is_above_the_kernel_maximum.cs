// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation.for_PageBounds;

public class when_the_client_bound_is_above_the_kernel_maximum : Specification
{
    PageBounds _bounds;

    void Because() => _bounds = PageBounds.For(
        new PageDeliveryDefinition(MaxEventsPerPage: 500, MaxBytesPerPage: 10_000_000),
        new Observers { MaxEventsPerPage = 100, MaxBytesPerSubscriberCall = 4096 });

    [Fact] void should_cap_the_event_bound() => _bounds.MaxEvents.ShouldEqual(100);
    [Fact] void should_cap_the_byte_bound() => _bounds.MaxBytes.ShouldEqual(4096);
}
