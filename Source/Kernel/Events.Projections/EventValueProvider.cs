// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Represents the delegate for providing a value from an event.
    /// </summary>
    /// <param name="event"><see cref="Event"/> to resolve from.</param>
    /// <returns>Resolved value.</returns>
    public delegate object EventValueProvider(Event @event);
}
