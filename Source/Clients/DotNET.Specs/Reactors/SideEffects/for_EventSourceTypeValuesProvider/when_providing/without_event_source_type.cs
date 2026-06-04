// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.SideEffects.for_EventSourceTypeValuesProvider.when_providing;

public class without_event_source_type : Specification
{
    EventSourceTypeValuesProvider _provider;
    ReactorContextValues _result;

    void Establish() => _provider = new EventSourceTypeValuesProvider();

    void Because() => _result = _provider.Provide(new PlainReactor(), EventContext.EmptyWithEventSourceId(EventSourceId.New()));

    [Fact] void should_not_return_any_values() => _result.ShouldBeEmpty();

    class PlainReactor : IReactor;
}
