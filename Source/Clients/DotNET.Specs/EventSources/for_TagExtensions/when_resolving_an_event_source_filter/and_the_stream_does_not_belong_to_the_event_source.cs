// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSources.for_TagExtensions.when_resolving_an_event_source_filter;

public class and_the_stream_does_not_belong_to_the_event_source : given.an_event_source_filter
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => typeof(PaymentReactor).GetEventSourceFilter(_eventSources));

    [Fact] void should_throw_an_error() => _error.ShouldBeOfExactType<EventStreamDoesNotBelongToEventSource>();
}
