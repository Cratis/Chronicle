// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Setup;

/// <summary>
/// Tracks and retries kernel projection registrations deferred during startup.
/// </summary>
public interface IKernelProjectionRegistrationRetries
{
    /// <summary>
    /// Gets a snapshot of event stores whose registration is pending.
    /// </summary>
    IReadOnlyCollection<EventStoreName> Pending { get; }

    /// <summary>
    /// Gets the number of background attempts for an event store.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <returns>The number of attempts, or zero when none are pending.</returns>
    int GetAttemptCount(EventStoreName eventStore);

    /// <summary>
    /// Schedules registration unless the store already has a retry loop or shutdown has begun.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    void Defer(EventStoreName eventStore);

    /// <summary>
    /// Cancels retry delays when the silo stops.
    /// </summary>
    void Stop();
}
