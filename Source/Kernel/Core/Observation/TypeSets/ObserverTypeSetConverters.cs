// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.TypeSets;

/// <summary>
/// Converts shared observer type-set values to their generated wire contracts.
/// </summary>
public static class ObserverTypeSetConverters
{
    /// <summary>
    /// Converts an initialization result.
    /// </summary>
    /// <param name="source">The result.</param>
    /// <returns>The wire result.</returns>
    public static Contracts.Observation.TypeSets.ObserverInitializationResult ToContract(this ObserverInitializationResult source) => new()
    {
        Namespace = source.Namespace,
        ObserverId = source.ObserverId,
        State = (Contracts.Observation.TypeSets.ObserverInitializationState)source.State,
        RefusalReasons = source.RefusalReasons.ToArray()
    };

    /// <summary>
    /// Converts a member.
    /// </summary>
    /// <param name="source">The member.</param>
    /// <returns>The wire member.</returns>
    public static Contracts.Observation.TypeSets.ObserverTypeSetMember ToContract(this ObserverTypeSetMember source) => new()
    {
        EventTypeId = source.EventTypeId,
        Incarnation = source.Incarnation,
        JoinFence = source.JoinFence
    };

    /// <summary>
    /// Converts a stored registration capability.
    /// </summary>
    /// <param name="source">The registration.</param>
    /// <returns>The wire registration.</returns>
    public static Contracts.Observation.TypeSets.ObserverTypeSetRegistration ToContract(this ObserverTypeSetRegistration source) => new()
    {
        ConnectionId = source.ConnectionId,
        RegistrationId = source.RegistrationId,
        DeclaredTypes = source.DeclaredTypes.Select(_ => _.ToContract()).ToArray()
    };

    /// <summary>
    /// Converts a declared event type.
    /// </summary>
    /// <param name="source">The declaration.</param>
    /// <returns>The wire declaration.</returns>
    public static Contracts.Observation.TypeSets.ObserverDeclaredEventType ToContract(this ObserverDeclaredEventType source) => new()
    {
        Id = source.Id,
        Generation = source.Generation,
        Tombstone = source.Tombstone
    };

    /// <summary>
    /// Converts a pending change.
    /// </summary>
    /// <param name="source">The change.</param>
    /// <returns>The wire change.</returns>
    public static Contracts.Observation.TypeSets.ObserverTypeSetChange ToContract(this ObserverTypeSetChange source) => new()
    {
        Revision = source.Revision,
        Started = source.Started,
        InitialTail = source.InitialTail,
        Additions = source.Additions.ToArray(),
        Retirements = source.Retirements.Select(_ => _.ToContract()).ToArray()
    };

    /// <summary>
    /// Converts a pending retirement request.
    /// </summary>
    /// <param name="source">The request.</param>
    /// <returns>The wire request.</returns>
    public static Contracts.Observation.TypeSets.ObserverEventTypeRetirementRequest ToContract(this ObserverEventTypeRetirementRequest source) => new()
    {
        OperationId = source.OperationId,
        EventTypeId = source.EventTypeId
    };
}
