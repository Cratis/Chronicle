// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// Represents the durable membership and registration capabilities of a namespace observer.
/// </summary>
/// <param name="Revision">The committed membership revision.</param>
/// <param name="Contract">The observer's fixed subscriber and filter contract.</param>
/// <param name="Members">Members keyed by event-type identifier, independently of generation.</param>
/// <param name="Registrations">The latest registration identity for each connection. Stored identities do not establish connectivity.</param>
public record ObserverTypeSetControl(
    ObserverTypeSetRevision Revision,
    ObserverCompatibilityContract Contract,
    IReadOnlyDictionary<EventTypeId, ObserverTypeSetMember> Members,
    IReadOnlyList<ObserverRegistrationIdentity> Registrations)
{
    /// <summary>
    /// Gets the retained final retirement results, absent in control written before retirement support.
    /// Results are never evicted or reused for a later incarnation.
    /// </summary>
    public IReadOnlyList<ObserverEventTypeRetirementResult>? RetirementResults { get; init; }

    /// <summary>
    /// Gets the pending membership intent, absent when no change is in progress.
    /// </summary>
    public ObserverTypeSetChange? PendingChange { get; init; }
}
