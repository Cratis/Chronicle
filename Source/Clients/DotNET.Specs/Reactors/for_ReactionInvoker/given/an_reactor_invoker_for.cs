// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.given;

public class an_reactor_invoker_for<TReactor> : Specification
{
    protected IEventTypes _eventTypes;
    protected IImmutableList<EventType> _reactorEventTypes;

    void Establish()
    {
        _eventTypes = new EventTypesForSpecifications(GetEventTypes());
        _reactorEventTypes = ReactorInvoker.GetEventTypesFor(_eventTypes, typeof(TReactor));
    }

    protected virtual IEnumerable<Type> GetEventTypes() => [];
}
