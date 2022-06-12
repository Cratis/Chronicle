// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Events.Projections;

/// <summary>
/// Exception that gets thrown when a target property only supports being mapped to a property on the event.
/// </summary>
public class OnlyEventPropertySupported : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OnlyEventPropertySupported"/> class.
    /// </summary>
    /// <param name="targetProperty"><see cref="PropertyPath"/> for the target property that requires this.</param>
    public OnlyEventPropertySupported(PropertyPath targetProperty) : base($"'{targetProperty}' can only be mapped to another property on event.")
    {
    }
}
