// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Registrations;

/// <summary>
/// Defines what a silo knows about the registrations it has accepted and not yet applied, for its metrics.
/// </summary>
public interface IRegistrationBacklog
{
    /// <summary>
    /// Reports the registrations of an event store that have been accepted and not yet applied.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/>.</param>
    /// <param name="pending">How many registrations are waiting.</param>
    /// <param name="oldestAcceptedAt">When the oldest of them was accepted; null when none is waiting.</param>
    void Report(EventStoreName eventStore, int pending, DateTimeOffset? oldestAcceptedAt);

    /// <summary>
    /// Stops reporting an event store, because this silo no longer holds its registrations.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/>.</param>
    void Forget(EventStoreName eventStore);
}
