// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.SideEffects.for_EventSourceTypeValuesProvider.when_providing;

public class with_event_source_type_attribute : Specification
{
    EventSourceTypeValuesProvider _provider;
    ReactorContextValues _result;

    void Establish() => _provider = new EventSourceTypeValuesProvider();

    void Because() => _result = _provider.Provide(new ReactorWithAttribute(), EventContext.EmptyWithEventSourceId(EventSourceId.New()));

    [Fact] void should_return_event_source_type_value() => _result.ContainsKey(WellKnownReactorContextKeys.EventSourceType).ShouldBeTrue();
    [Fact] void should_have_the_attribute_event_source_type() => ((EventSourceType)_result[WellKnownReactorContextKeys.EventSourceType]).ShouldEqual((EventSourceType)"Customer");

    [EventSourceType("Customer")]
    class ReactorWithAttribute : IReactor;
}
