// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Applications.Commands;

/// <summary>
/// Represents an implementation of <see cref="ICommandContextValuesProvider"/> that provides values for the event source id.
/// </summary>
public class EventSourceValuesProvider : ICommandContextValuesProvider
{
    /// <inheritdoc/>
    public CommandContextValues Provide(object command)
    {
        if (command is ICanProvideEventSourceId provider)
        {
            return new CommandContextValues
            {
                { WellKnownCommandContextKeys.EventSourceId, provider.GetEventSourceId() }
            };
        }

        var eventSourceId = EventSourceId.New();
        if (command.HasEventSourceId())
        {
            eventSourceId = command.GetEventSourceId();
        }

        return new CommandContextValues
        {
            { WellKnownCommandContextKeys.EventSourceId, eventSourceId }
        };
    }
}
