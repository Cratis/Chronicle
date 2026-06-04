// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.SideEffects.for_EventStreamIdValuesProvider.when_providing;

public class with_interface_implementation : Specification
{
    EventStreamIdValuesProvider _provider;
    ReactorContextValues _result;
    EventStreamId _eventStreamId;

    void Establish()
    {
        _provider = new EventStreamIdValuesProvider();
        _eventStreamId = "Yearly";
    }

    void Because() => _result = _provider.Provide(new ReactorWithEventStreamId(_eventStreamId), EventContext.EmptyWithEventSourceId(EventSourceId.New()));

    [Fact] void should_return_event_stream_id_value() => _result.ContainsKey(WellKnownReactorContextKeys.EventStreamId).ShouldBeTrue();
    [Fact] void should_have_the_provided_event_stream_id() => ((EventStreamId)_result[WellKnownReactorContextKeys.EventStreamId]).ShouldEqual(_eventStreamId);

    class ReactorWithEventStreamId(EventStreamId eventStreamId) : IReactor, ICanProvideEventStreamId
    {
        public EventStreamId GetEventStreamId() => eventStreamId;
    }
}
