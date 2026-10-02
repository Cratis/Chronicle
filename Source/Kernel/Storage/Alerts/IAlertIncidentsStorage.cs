// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Storage.Alerts;

/// <summary>
/// Stores retained incident rows in the System store Default namespace.
/// </summary>
public interface IAlertIncidentsStorage
{
    /// <summary>
    /// Applies one atomic conditional mutation.
    /// </summary>
    /// <param name="transition">Transition.</param>
    /// <param name="cancellationToken">CancellationToken.</param>
    /// <returns>The confirmed result.</returns>
    Task<AlertIncidentWriteOutcome> Apply(AlertIncidentTransition transition, CancellationToken cancellationToken = default);

    /// <summary>
    /// Looks up an open row within the affected scope.
    /// </summary>
    /// <param name="scope">Scope.</param>
    /// <param name="incidentId">IncidentId.</param>
    /// <param name="cancellationToken">CancellationToken.</param>
    /// <returns>The confirmed result.</returns>
    Task<AlertIncident?> GetOpen(AlertIncidentScope scope, IncidentId incidentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pages matching open incidents in canonical keyset order.
    /// </summary>
    /// <param name="filter">Filter.</param>
    /// <param name="after">After.</param>
    /// <param name="limit">Limit.</param>
    /// <param name="cancellationToken">CancellationToken.</param>
    /// <returns>The confirmed result.</returns>
    Task<AlertIncidentStoragePage> GetOpenPage(AlertIncidentFilter filter, AlertIncidentCursor? after, int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts the complete matching open population.
    /// </summary>
    /// <param name="scope">Scope.</param>
    /// <param name="cancellationToken">CancellationToken.</param>
    /// <returns>The confirmed result.</returns>
    Task<IEnumerable<AlertIncidentCount>> GetOpenCounts(AlertIncidentScope scope, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enumerates open incidents internally; this is not a public query.
    /// </summary>
    /// <param name="after">After.</param>
    /// <param name="limit">Limit.</param>
    /// <param name="cancellationToken">CancellationToken.</param>
    /// <returns>The confirmed result.</returns>
    Task<AlertIncidentStoragePage> EnumerateOpen(AlertIncidentCursor? after, int limit, CancellationToken cancellationToken = default);
}
