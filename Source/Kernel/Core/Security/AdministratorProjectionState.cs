// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using StoredAdministrator = Cratis.Chronicle.Storage.Security.Administrator;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Recomputes projected privileges from the authoritative system roster, not delivery order.
/// </summary>
/// <param name="storage">The system event storage.</param>
/// <param name="serializer">The event serializer.</param>
/// <param name="proof">The kernel event proof.</param>
internal class AdministratorProjectionState(IStorage storage, IEventSerializer serializer, IKernelEventProof proof)
{
    IEventSequenceStorage Events => storage.GetEventStore(EventStoreName.System)
        .GetNamespace(EventStoreNamespaceName.Default).GetEventSequence(EventSequenceId.Log);

    /// <summary>
    /// Computes the administrator state for a user from the authoritative System log.
    /// </summary>
    /// <param name="userId">The user whose state is requested.</param>
    /// <returns>The administrator if the user holds verified privilege; otherwise null.</returns>
    internal async Task<StoredAdministrator?> Get(UserId userId)
    {
        var tail = await Events.GetTailSequenceNumber(RosterHistory.QueryTypes);
        if (!tail.IsActualValue) return null;
        var history = new RosterHistory(proof);
        using var cursor = await Events.GetRange(EventSequenceNumber.First, tail, eventTypes: RosterHistory.QueryTypes);
        while (await cursor.MoveNext())
        {
            foreach (var @event in cursor.Current)
            {
                var context = @event.Context;
                if (!string.Equals(context.EventStore.Value, EventStoreName.System.Value, StringComparison.OrdinalIgnoreCase) ||
                    context.Namespace != EventStoreNamespaceName.Default)
                {
                    continue;
                }

                // Roster slots count in every stream. Legacy user removals keep their user-stream rule.
                if (context.EventType.Id == typeof(UserRemoved).GetEventType().Id && !AdministratorEventScope.IsSystemEvent(context)) continue;
                history.Apply(@event, serializer.Deserialize);
            }
        }
        return history.Granted.Contains(userId) && history.LatestGrants.TryGetValue(userId, out var grant)
            ? new StoredAdministrator(userId, grant.Reason, grant.Evidence)
            : null;
    }
}
