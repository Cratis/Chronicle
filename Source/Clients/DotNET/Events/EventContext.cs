// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events
{
    /// <summary>
    /// Represents the context surrounding an event.
    /// </summary>
    /// <param name="EventSourceId">The <see cref="EventSourceId"/> the event is for.</param>
    /// <param name="Occurred">When it <see cref="DateTimeOffset">occurred</see>.</param>
    public record EventContext(EventSourceId EventSourceId, DateTimeOffset Occurred);
}
