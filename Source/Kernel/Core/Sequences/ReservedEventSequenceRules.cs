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
    /// Check whether a write may create an event sequence with a reserved name, without touching storage.
    /// </summary>
    /// <param name="reservedEventSequenceIds">The <see cref="IReservedEventSequenceIds"/> of the active storage provider.</param>
    /// <param name="eventStore">The event store name.</param>
    /// <param name="namespace">The namespace name.</param>
    /// <param name="eventSequenceId">The event sequence identifier.</param>
    /// <returns>True when the name is reserved and storage has to be asked whether the event sequence already exists.</returns>
    /// <remarks>
    /// This is the common-case check; it does not allocate. Blank values and values holding the key separator are left
    /// to the required-value and key-separator rules.
    /// </remarks>
    public static bool MayBeReserved(
        IReservedEventSequenceIds reservedEventSequenceIds,
        EventStoreName? eventStore,
        EventStoreNamespaceName? @namespace,
        EventSequenceId? eventSequenceId)
    {
        var eventStoreValue = eventStore?.Value;
        var namespaceValue = @namespace?.Value;
        var eventSequenceIdValue = eventSequenceId?.Value;
        if (string.IsNullOrWhiteSpace(eventStoreValue) ||
            string.IsNullOrWhiteSpace(namespaceValue) ||
            string.IsNullOrWhiteSpace(eventSequenceIdValue) ||
            eventStoreValue.Contains(KeyHelper.Separator) ||
            namespaceValue.Contains(KeyHelper.Separator) ||
            eventSequenceIdValue.Contains(KeyHelper.Separator))
        {
            return false;
        }

        return !IsBuiltIn(eventSequenceIdValue) && reservedEventSequenceIds.IsReserved(eventSequenceId!);
    }

    /// <summary>
    /// Check whether an event sequence already exists. An unknown event store is never created by the lookup.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> to read existing event sequences from.</param>
    /// <param name="eventStore">The event store name.</param>
    /// <param name="namespace">The namespace name.</param>
    /// <param name="eventSequenceId">The event sequence identifier.</param>
    /// <returns>True when the event sequence exists.</returns>
    public static async Task<bool> Exists(
        IStorage storage,
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        EventSequenceId eventSequenceId)
    {
        var stores = (await storage.GetEventStores()).ToArray();
        var storeName = stores.FirstOrDefault(_ => string.Equals(_.Value, eventStore.Value, StringComparison.Ordinal));
        if (storeName is null)
        {
            var matches = stores.Where(_ => string.Equals(_.Value, eventStore.Value, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (matches.Length != 1)
            {
                return false;
            }

            storeName = matches[0];
        }

        var existing = await storage.GetEventStore(storeName).GetNamespace(@namespace).GetEventSequences();
        return existing.Contains(eventSequenceId);
    }

    /// <summary>
    /// Check whether an event sequence may be written to.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> to read existing event sequences from.</param>
    /// <param name="reservedEventSequenceIds">The <see cref="IReservedEventSequenceIds"/> of the active storage provider.</param>
    /// <param name="eventStore">The event store name.</param>
    /// <param name="namespace">The namespace name.</param>
    /// <param name="eventSequenceId">The event sequence identifier.</param>
    /// <returns>True when the write may proceed, false when it would create a new event sequence with a reserved name.</returns>
    public static async Task<bool> MayBeWritten(
        IStorage storage,
        IReservedEventSequenceIds reservedEventSequenceIds,
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        EventSequenceId eventSequenceId) =>
        !MayBeReserved(reservedEventSequenceIds, eventStore, @namespace, eventSequenceId) ||
        await Exists(storage, eventStore, @namespace, eventSequenceId);

    static bool IsBuiltIn(string eventSequenceId) =>
        string.Equals(eventSequenceId, EventSequenceId.Log.Value, StringComparison.Ordinal) ||
        string.Equals(eventSequenceId, EventSequenceId.System.Value, StringComparison.Ordinal) ||
        string.Equals(eventSequenceId, EventSequenceId.Outbox.Value, StringComparison.Ordinal) ||
        string.Equals(eventSequenceId, EventSequenceId.Inbox.Value, StringComparison.Ordinal);
}
