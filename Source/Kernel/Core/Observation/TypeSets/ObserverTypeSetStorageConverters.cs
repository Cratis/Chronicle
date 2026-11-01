// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.TypeSets;

/// <summary>
/// Maps durable observer type-set state to its management read model.
/// </summary>
internal static class ObserverTypeSetStorageConverters
{
    /// <summary>
    /// Maps the stored state, preserving the distinction between membership and registration capabilities.
    /// </summary>
    /// <param name="state">The stored observer state, if present.</param>
    /// <returns>The management read model.</returns>
    public static ObserverTypeSet ToReadModel(Storage.Observation.ObserverState? state)
    {
        var control = state?.TypeSetControl;
        var initialization = state?.Initialization;
        return new(
            control?.Revision.Value ?? 0,
            control?.Members.Select(_ => new ObserverTypeSetMember(_.Key.Value, _.Value.Incarnation.Value, _.Value.JoinFence is { } fence ? new ObserverTypeSetFence(fence.Value) : null)).ToArray() ?? [],
            control?.Registrations.Select(ToReadModel).ToArray() ?? [],
            control?.PendingChange is { } change ? ToReadModel(change) : null,
            control?.RetirementResults?.Select(ToReadModel).ToArray() ?? [],
            initialization is null ? ObserverInitializationState.NotInitialized : (ObserverInitializationState)initialization.State,
            initialization?.RefusalReasons.ToArray() ?? []);
    }

    static ObserverTypeSetRegistration ToReadModel(Storage.Observation.ObserverRegistrationIdentity registration) => new(
        registration.ConnectionId.Value,
        registration.RegistrationId,
        registration.DeclaredTypes.Select(_ => new ObserverDeclaredEventType(_.Id.Value, _.Generation.Value, _.Tombstone)).ToArray());

    static ObserverTypeSetChange ToReadModel(Storage.Observation.ObserverTypeSetChange change) => new(
        change.Revision.Value,
        change.Started,
        change.InitialTail.Value,
        change.Additions.Select(_ => _.Value).ToArray(),
        change.Retirements.Select(_ => new ObserverEventTypeRetirementRequest(_.OperationId, _.EventTypeId.Value)).ToArray());

    static ObserverEventTypeRetirement ToReadModel(Storage.Observation.ObserverEventTypeRetirementResult result) => new(
        result.OperationId,
        result.EventTypeId.Value,
        (ObserverEventTypeRetirementOutcome)result.Outcome,
        result.Incarnation is { } incarnation ? new ObserverTypeSetIncarnation(incarnation.Value) : null,
        result.DeclaringConnections.Select(_ => _.Value).ToArray());
}
