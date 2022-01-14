// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Defines a cursor API for walking through events.
    /// </summary>
    public interface IEventCursor
    {
        /// <summary>
        /// Move to the next part in the cursor.
        /// </summary>
        Task<bool>  MoveNext();

        /// <summary>
        /// Gets the current events, if any.
        /// </summary>
        IEnumerable<Event> Current { get; }
    }
}
