// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events
{
    /// <summary>
    /// Represents the type of an event.
    /// </summary>
    /// <param name="EventTypeId"><see cref="Events.EventTypeId">Unique identifier</see>.</param>
    /// <param name="Generation"><see cref="EventGeneration">Generation</see> of the event.</param>
    public record EventType(EventTypeId EventTypeId, EventGeneration Generation)
    {
        /// <inheritdoc/>
        public override string ToString()
        {
            return $"({EventTypeId.Value} - {Generation.Value})";
        }
    }
}