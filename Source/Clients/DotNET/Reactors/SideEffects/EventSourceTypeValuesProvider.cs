// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.SideEffects;

/// <summary>
/// Represents an implementation of <see cref="IReactorContextValuesProvider"/> that provides the event source type.
/// </summary>
/// <remarks>
/// Reads the <see cref="EventSourceTypeAttribute"/> from the reactor type. No value is provided when the attribute is absent.
/// </remarks>
public class EventSourceTypeValuesProvider : IReactorContextValuesProvider
{
    /// <inheritdoc/>
    public ReactorContextValues Provide(object reactor, EventContext eventContext)
    {
        var attribute = Attribute.GetCustomAttribute(reactor.GetType(), typeof(EventSourceTypeAttribute)) as EventSourceTypeAttribute;
        if (attribute is not null)
        {
            return new ReactorContextValues
            {
                { WellKnownReactorContextKeys.EventSourceType, attribute.EventSourceType }
            };
        }

        return [];
    }
}
