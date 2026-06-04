// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.SideEffects.for_EventSourceIdValuesProvider.when_providing;

public class without_interface_implementation : Specification
{
    EventSourceIdValuesProvider _provider;
    ReactorContextValues _result;
    EventSourceId _eventSourceId;

    void Establish()
    {
        _provider = new EventSourceIdValuesProvider();
        _eventSourceId = EventSourceId.New();
    }

    void Because() => _result = _provider.Provide(new PlainReactor(), EventContext.EmptyWithEventSourceId(_eventSourceId));

    [Fact] void should_return_event_source_id_value() => _result.ContainsKey(WellKnownReactorContextKeys.EventSourceId).ShouldBeTrue();
    [Fact] void should_fall_back_to_the_event_context_event_source_id() => ((EventSourceId)_result[WellKnownReactorContextKeys.EventSourceId]).ShouldEqual(_eventSourceId);

    class PlainReactor : IReactor;
}
