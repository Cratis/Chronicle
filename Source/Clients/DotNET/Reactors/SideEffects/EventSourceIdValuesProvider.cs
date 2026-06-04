// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.SideEffects;

/// <summary>
/// Represents an implementation of <see cref="IReactorContextValuesProvider"/> that provides the event source id.
/// </summary>
/// <remarks>
/// Resolution order: the reactor implementing <see cref="ICanProvideEventSourceId"/> takes priority,
/// otherwise the <see cref="EventSourceId"/> from the triggering <see cref="EventContext"/> is used.
/// </remarks>
public class EventSourceIdValuesProvider : IReactorContextValuesProvider
{
    /// <inheritdoc/>
    public ReactorContextValues Provide(object reactor, EventContext eventContext)
    {
        var eventSourceId = reactor is ICanProvideEventSourceId provider
            ? provider.GetEventSourceId()
            : eventContext.EventSourceId;

        return new ReactorContextValues
        {
            { WellKnownReactorContextKeys.EventSourceId, eventSourceId }
        };
    }
}
