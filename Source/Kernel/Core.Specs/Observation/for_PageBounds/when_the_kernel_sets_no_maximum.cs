// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation.for_PageBounds;

public class when_the_kernel_sets_no_maximum : Specification
{
    PageBounds _declared;
    PageBounds _undeclared;

    void Because()
    {
        var configuration = new Observers { MaxEventsPerSubscriberCall = 0, MaxBytesPerSubscriberCall = 0 };
        _declared = PageBounds.For(new PageDeliveryDefinition(MaxEventsPerPage: 5000, MaxBytesPerPage: 123), configuration);
        _undeclared = PageBounds.For(new PageDeliveryDefinition(), configuration);
    }

    [Fact] void should_use_the_declared_event_bound() => _declared.MaxEvents.ShouldEqual(5000);
    [Fact] void should_use_the_declared_byte_bound() => _declared.MaxBytes.ShouldEqual(123);
    [Fact] void should_leave_an_undeclared_page_unbounded() => _undeclared.IsBounded.ShouldBeFalse();
}
