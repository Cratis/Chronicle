// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events;

/// <summary>
/// Defines a branch from <see cref="IEventLog"/>.
/// </summary>
public interface IBranch
{
    /// <summary>
    /// Gets the identifier that identifies which type of branch this is.
    /// </summary>
    BranchTypeId Type { get; }

    /// <summary>
    /// Gets the unique identifier of the branch.
    /// </summary>
    BranchId Identifier { get; }

    /// <summary>
    /// Append a single event to the event store.
    /// </summary>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to append for.</param>
    /// <param name="event">The event.</param>
    /// /// <param name="validFrom">Optional date and time for when the event is valid from. </param>
    /// <returns>Awaitable <see cref="Task"/>.</returns>
    Task Append(EventSourceId eventSourceId, object @event, DateTimeOffset? validFrom = default);

    /// <summary>
    /// Merge the branch into the <see cref="IEventLog"/>.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Merge();
}
