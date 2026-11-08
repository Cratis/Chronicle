// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation.for_PageBounds;

public class when_the_client_bound_is_below_the_kernel_maximum : Specification
{
    PageBounds _bounds;

    void Because() => _bounds = PageBounds.For(
        new PageDeliveryDefinition(MaxEventsPerPage: 10, MaxBytesPerPage: 1000),
        new Observers { MaxEventsPerSubscriberCall = 100, MaxBytesPerSubscriberCall = 4096 });

    [Fact] void should_use_the_declared_event_bound() => _bounds.MaxEvents.ShouldEqual(10);
    [Fact] void should_use_the_declared_byte_bound() => _bounds.MaxBytes.ShouldEqual(1000);
}
