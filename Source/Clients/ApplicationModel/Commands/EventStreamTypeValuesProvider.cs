// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Chronicle.Applications.Commands;

/// <summary>
/// Represents an implementation of <see cref="ICommandContextValuesProvider"/> that provides values for the event stream type.
/// </summary>
public class EventStreamTypeValuesProvider : ICommandContextValuesProvider
{
    /// <inheritdoc/>
    public CommandContextValues Provide(object command)
    {
        var commandType = command.GetType();
        var attribute = commandType.GetCustomAttributes(typeof(EventStreamTypeAttribute), false).FirstOrDefault() as EventStreamTypeAttribute;

        if (attribute is not null)
        {
            return new CommandContextValues
            {
                { WellKnownCommandContextKeys.EventStreamType, attribute.Value }
            };
        }

        return [];
    }
}
