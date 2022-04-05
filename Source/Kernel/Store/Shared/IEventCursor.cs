// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store;

/// <summary>
/// Defines a cursor API for walking through events.
/// </summary>
public interface IEventCursor : IDisposable
{
    /// <summary>
    /// Move to the next part in the cursor.
    /// </summary>
    /// <returns>True if can move next, false if not.</returns>
    Task<bool> MoveNext();

    /// <summary>
    /// Gets the current events, if any.
    /// </summary>
    /// <returns>Collection of current <see cref="AppendedEvent">events</see>.</returns>
    IEnumerable<AppendedEvent> Current { get; }
}
