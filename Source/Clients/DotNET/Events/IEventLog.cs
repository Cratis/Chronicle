// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events;

/// <summary>
/// Defines the client event log.
/// </summary>
public interface IEventLog
{
    /// <summary>
    /// Append a single event to the event store.
    /// </summary>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to append for.</param>
    /// <param name="event">The event.</param>
    /// <param name="validFrom">Optional date and time for when the event is valid from. </param>
    /// <returns>Awaitable <see cref="Task"/>.</returns>
    Task Append(EventSourceId eventSourceId, object @event, DateTimeOffset? validFrom = default);

    /// <summary>
    /// Start a branch for a specific <see cref="BranchTypeId"/>.
    /// </summary>
    /// <param name="branchTypeId">Type to start.</param>
    /// <param name="labels">Optional labels to associate with the branch.</param>
    /// <returns><see cref="IBranch"/>.</returns>
    Task<IBranch> Branch(BranchTypeId branchTypeId, IDictionary<string, string>? labels = default);

    /// <summary>
    /// Get branches of a specific <see cref="BranchTypeId"/>.
    /// </summary>
    /// <param name="branchTypeId">Type to get for.</param>
    /// <returns>Collection of <see cref="IBranch"/>.</returns>
    Task<IEnumerable<IBranch>> GetBranchesFor(BranchTypeId branchTypeId);
}
