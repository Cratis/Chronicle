// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Applications.Commands;

/// <summary>
/// Represents an implementation of <see cref="ICommandContextValuesProvider"/> that provides values for the event stream id.
/// </summary>
public class EventStreamIdValuesProvider : ICommandContextValuesProvider
{
    /// <inheritdoc/>
    public CommandContextValues Provide(object command)
    {
        var commandType = command.GetType();
        var attribute = commandType.GetCustomAttributes(typeof(EventStreamIdAttribute), false).FirstOrDefault() as EventStreamIdAttribute;
        var implementsInterface = command is ICanProvideEventStreamId;

        if (attribute is not null && attribute.Value != EventStreamId.NotSet && implementsInterface)
        {
            throw new AmbiguousEventStreamId(commandType);
        }

        if (implementsInterface)
        {
            var provider = (ICanProvideEventStreamId)command;
            return new CommandContextValues
            {
                { WellKnownCommandContextKeys.EventStreamId, provider.GetEventStreamId() }
            };
        }

        if (attribute is not null && attribute.Value != EventStreamId.NotSet)
        {
            return new CommandContextValues
            {
                { WellKnownCommandContextKeys.EventStreamId, attribute.Value }
            };
        }

        return [];
    }
}
