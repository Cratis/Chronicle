// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.SideEffects;

/// <summary>
/// Represents an implementation of <see cref="IReactorContextValuesProvider"/> that provides the event stream type.
/// </summary>
/// <remarks>
/// Reads the <see cref="EventStreamTypeAttribute"/> from the reactor type. No value is provided when the attribute is absent.
/// </remarks>
public class EventStreamTypeValuesProvider : IReactorContextValuesProvider
{
    /// <inheritdoc/>
    public ReactorContextValues Provide(object reactor, EventContext eventContext)
    {
        var attribute = Attribute.GetCustomAttribute(reactor.GetType(), typeof(EventStreamTypeAttribute)) as EventStreamTypeAttribute;
        if (attribute is not null)
        {
            return new ReactorContextValues
            {
                { WellKnownReactorContextKeys.EventStreamType, attribute.EventStreamType }
            };
        }

        return [];
    }
}
