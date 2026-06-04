// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.SideEffects;

/// <summary>
/// Represents an implementation of <see cref="IReactorContextValuesProvider"/> that provides the event stream id.
/// </summary>
/// <remarks>
/// Resolution order: the reactor implementing <see cref="ICanProvideEventStreamId"/> takes priority,
/// otherwise the <see cref="EventStreamIdAttribute"/> value is used when it is not <see cref="EventStreamId.NotSet"/>.
/// When neither is present, no value is provided.
/// </remarks>
public class EventStreamIdValuesProvider : IReactorContextValuesProvider
{
    /// <inheritdoc/>
    public ReactorContextValues Provide(object reactor, EventContext eventContext)
    {
        if (reactor is ICanProvideEventStreamId provider)
        {
            return new ReactorContextValues
            {
                { WellKnownReactorContextKeys.EventStreamId, provider.GetEventStreamId() }
            };
        }

        var attribute = Attribute.GetCustomAttribute(reactor.GetType(), typeof(EventStreamIdAttribute)) as EventStreamIdAttribute;
        if (attribute is not null && attribute.Value != EventStreamId.NotSet)
        {
            return new ReactorContextValues
            {
                { WellKnownReactorContextKeys.EventStreamId, attribute.Value }
            };
        }

        return [];
    }
}
