// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Delegate that lets one access the actual instance to work with.
    /// </summary>
    /// <param name="initialState">The initial state for accessing the instance from.</param>
    /// <param name="event">The event we're getting in context from.</param>
    /// <param name="keyResolver"><see cref="EventValueProvider"/> for any resolutions based on a value within an <see cref="Event"/>.</param>
    /// <returns>The actual <see cref="ExpandoObject"/>.</returns>
    public delegate ExpandoObject InstanceAccessor(ExpandoObject initialState, Event @event, EventValueProvider keyResolver);
}
