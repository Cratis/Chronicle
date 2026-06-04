// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.SideEffects.for_EventStreamIdValuesProvider.when_providing;

public class with_event_stream_id_attribute : Specification
{
    EventStreamIdValuesProvider _provider;
    ReactorContextValues _result;

    void Establish() => _provider = new EventStreamIdValuesProvider();

    void Because() => _result = _provider.Provide(new ReactorWithAttribute(), EventContext.EmptyWithEventSourceId(EventSourceId.New()));

    [Fact] void should_return_event_stream_id_value() => _result.ContainsKey(WellKnownReactorContextKeys.EventStreamId).ShouldBeTrue();
    [Fact] void should_have_the_attribute_event_stream_id() => ((EventStreamId)_result[WellKnownReactorContextKeys.EventStreamId]).ShouldEqual((EventStreamId)"Yearly");

    [EventStreamId("Yearly")]
    class ReactorWithAttribute : IReactor;
}
