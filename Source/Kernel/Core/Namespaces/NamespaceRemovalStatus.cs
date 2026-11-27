// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Namespaces;

/// <summary>An authorized diagnostic snapshot; inspecting it never creates or ensures a namespace.</summary>
/// <param name="Namespace">The canonical namespace.</param>
/// <param name="Status">The durable lifecycle status.</param>
/// <param name="OperationId">The removal operation.</param>
/// <param name="Revision">The revision required by conditional operator commands.</param>
/// <param name="Stage">The next unacknowledged checkpoint.</param>
/// <param name="StageAttempts">The attempts at this checkpoint.</param>
/// <param name="TotalAttempts">The retained historical attempt count.</param>
/// <param name="BudgetStartAttempts">The historical count at the latest budget's start.</param>
/// <param name="AttemptInProgress">Whether an effect lacks a terminal acknowledgement.</param>
/// <param name="DestructiveAttemptOutstanding">Whether a destructive effect is unresolved.</param>
/// <param name="DestructiveActionIssued">Whether abort is permanently unsafe.</param>
/// <param name="LastFailure">The last stable diagnostic, never an exception message.</param>
[ReadModel]
[BelongsTo(WellKnownServices.Namespaces)]
[StoreAccess(StoreAccess.Manage)]
[StoreCapability("chronicle.event-store.manage")]
public record NamespaceRemovalStatus(
    string Namespace,
    string Status,
    Guid OperationId,
    long Revision,
    string Stage,
    int StageAttempts,
    int TotalAttempts,
    int BudgetStartAttempts,
    bool AttemptInProgress,
    bool DestructiveAttemptOutstanding,
    bool DestructiveActionIssued,
    string LastFailure)
{
    /// <summary>Reads the operator snapshot without ensuring, creating or reopening anything.</summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="incarnations">The capability-gated namespace owners.</param>
    /// <returns>The persisted operator snapshot.</returns>
    /// <exception cref="NamespaceIncarnationUnavailable">No lifecycle exists for this namespace.</exception>
    public static async Task<NamespaceRemovalStatus> GetNamespaceRemovalStatus(EventStoreName eventStore, EventStoreNamespaceName @namespace, INamespaceIncarnations incarnations)
    {
        var current = await incarnations.GetStorage(eventStore).NamespaceLifecycles.Get(@namespace) ?? throw new NamespaceIncarnationUnavailable(eventStore);
        return NamespaceRemovalStatusConverters.ToStatus(current);
    }
}
