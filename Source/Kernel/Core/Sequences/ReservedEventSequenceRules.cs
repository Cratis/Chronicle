// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Holds the shared rule keeping new event sequences off the names the storage provider keeps for itself.
/// </summary>
/// <remarks>
/// An event sequence is stored under its identifier, so a new sequence named like an internal collection or table
/// would write its events into internal data. Built-in sequences and sequences that already exist are unaffected.
/// </remarks>
internal static class ReservedEventSequenceRules
{
    /// <summary>
    /// Gets the message reported when a new event sequence uses a reserved name.
    /// </summary>
    /// <param name="eventSequenceId">The refused event sequence identifier.</param>
    /// <returns>The message.</returns>
    public static string Message(string eventSequenceId) =>
        $"The event sequence name '{eventSequenceId}' is reserved for Chronicle's own storage and cannot be used for a new event sequence. Choose a different name.";

    /// <summary>
    /// Check whether an event sequence may be written to.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> to read existing event sequences from.</param>
    /// <param name="reservedEventSequenceIds">The <see cref="IReservedEventSequenceIds"/> of the active storage provider.</param>
    /// <param name="eventStore">The event store name.</param>
    /// <param name="namespace">The namespace name.</param>
    /// <param name="eventSequenceId">The event sequence identifier.</param>
    /// <returns>True when the write may proceed, false when it would create a new event sequence with a reserved name.</returns>
    /// <remarks>
    /// The common case is a pure in-memory name check. Storage is only consulted for a reserved name, and an unknown
    /// event store is never created by the lookup.
    /// </remarks>
    public static async Task<bool> MayBeWritten(
        IStorage storage,
        IReservedEventSequenceIds reservedEventSequenceIds,
        string eventStore,
        string @namespace,
        string eventSequenceId)
    {
        if (string.IsNullOrEmpty(eventStore) ||
            string.IsNullOrEmpty(@namespace) ||
            string.IsNullOrEmpty(eventSequenceId) ||
            eventStore.Contains(KeyHelper.Separator) ||
            @namespace.Contains(KeyHelper.Separator) ||
            eventSequenceId.Contains(KeyHelper.Separator))
        {
            return true;
        }

        if (IsBuiltIn(eventSequenceId))
        {
            return true;
        }

        var id = new EventSequenceId(eventSequenceId);
        if (!reservedEventSequenceIds.IsReserved(id))
        {
            return true;
        }

        var stores = (await storage.GetEventStores()).ToArray();
        var storeName = stores.FirstOrDefault(_ => string.Equals(_.Value, eventStore, StringComparison.Ordinal));
        if (storeName is null)
        {
            var matches = stores.Where(_ => string.Equals(_.Value, eventStore, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (matches.Length != 1)
            {
                return false;
            }

            storeName = matches[0];
        }

        var existing = await storage.GetEventStore(storeName).GetNamespace((EventStoreNamespaceName)@namespace).GetEventSequences();
        return existing.Contains(id);
    }

    static bool IsBuiltIn(string eventSequenceId) =>
        string.Equals(eventSequenceId, EventSequenceId.Log.Value, StringComparison.Ordinal) ||
        string.Equals(eventSequenceId, EventSequenceId.System.Value, StringComparison.Ordinal) ||
        string.Equals(eventSequenceId, EventSequenceId.Outbox.Value, StringComparison.Ordinal) ||
        string.Equals(eventSequenceId, EventSequenceId.Inbox.Value, StringComparison.Ordinal);
}
