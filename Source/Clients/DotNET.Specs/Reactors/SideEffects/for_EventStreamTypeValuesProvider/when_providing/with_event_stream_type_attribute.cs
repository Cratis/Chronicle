// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.SideEffects.for_EventStreamTypeValuesProvider.when_providing;

public class with_event_stream_type_attribute : Specification
{
    EventStreamTypeValuesProvider _provider;
    ReactorContextValues _result;

    void Establish() => _provider = new EventStreamTypeValuesProvider();

    void Because() => _result = _provider.Provide(new ReactorWithAttribute(), EventContext.EmptyWithEventSourceId(EventSourceId.New()));

    [Fact] void should_return_event_stream_type_value() => _result.ContainsKey(WellKnownReactorContextKeys.EventStreamType).ShouldBeTrue();
    [Fact] void should_have_the_attribute_event_stream_type() => ((EventStreamType)_result[WellKnownReactorContextKeys.EventStreamType]).ShouldEqual((EventStreamType)"Audit");

    [EventStreamType("Audit")]
    class ReactorWithAttribute : IReactor;
}
