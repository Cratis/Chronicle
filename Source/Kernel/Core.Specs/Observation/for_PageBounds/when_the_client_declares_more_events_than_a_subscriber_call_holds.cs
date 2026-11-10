// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation.for_PageBounds;

/// <summary>
/// The page maximum, not the bound on catch-up subscriber calls, caps what a client declares, so declaring pages never
/// makes them smaller than a path hands over.
/// </summary>
public class when_the_client_declares_more_events_than_a_subscriber_call_holds : Specification
{
    PageBounds _bounds;

    void Because() => _bounds = PageBounds.For(
        new PageDeliveryDefinition(MaxEventsPerPage: 500),
        new Observers { MaxEventsPerSubscriberCall = 100 });

    [Fact] void should_keep_the_declared_event_bound() => _bounds.MaxEvents.ShouldEqual(500);
}
