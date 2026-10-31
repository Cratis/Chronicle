// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Verifies bootstrap provenance using the first bootstrap event, never caller identity.
/// </summary>
/// <param name="storage">The system event and user storage.</param>
/// <param name="serializer">The event serializer.</param>
public class AdministratorProvenance(IStorage storage, IEventSerializer serializer)
{
    IEventSequenceStorage SystemEventLog => storage.GetEventStore(EventStoreName.System)
        .GetNamespace(EventStoreNamespaceName.Default).GetEventSequence(EventSequenceId.Log);

    /// <summary>
    /// Finds the proven bootstrap user, including random identifiers from older versions.
    /// </summary>
    /// <param name="username">The configured administrator username.</param>
    /// <returns>The proven user, or null when any required evidence is absent.</returns>
    public async Task<UserId?> GetProvenAdministrator(Username username)
    {
        var events = SystemEventLog;
        var first = await events.GetNextSequenceNumberGreaterOrEqualThan(
            EventSequenceNumber.First, [typeof(InitialAdminUserAdded).GetEventType()]);
        if (!first.IsActualValue)
        {
            return null;
        }

        var appended = await events.GetEventAt(first);
        var bootstrap = (InitialAdminUserAdded)serializer.Deserialize(appended);
        if (!string.Equals(bootstrap.Username.Value, username.Value, StringComparison.OrdinalIgnoreCase) ||
            !Guid.TryParse(appended.Context.EventSourceId.Value, out var identifier))
        {
            return null;
        }

        UserId userId = identifier;
        var user = await storage.System.Users.GetById(userId);
        if (user?.IsActive is not true)
        {
            return null;
        }

        var removed = await events.GetNextSequenceNumberGreaterOrEqualThan(
            first + 1, [typeof(UserRemoved).GetEventType()], appended.Context.EventSourceId);
        return removed.IsActualValue ? null : userId;
    }
}
