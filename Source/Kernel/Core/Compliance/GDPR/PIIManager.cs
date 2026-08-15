// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.GDPR;

/// <summary>
/// Represents a manager of PII in the system.
/// </summary>
/// <remarks>
/// <para>
/// Initializes a new instance of the <see cref="PIIManager"/> class.
/// </para>
/// <para>
/// Every operation here runs across every event store in the manager's namespace. That is not a convenience: the
/// cross-event-store subscription copies a subject's key into the store it forwards into whenever that store has
/// none, so a key erased in one event store is put back by the next forwarded event unless the erasure reached the
/// store that still had it - and reached it in a way that refuses the copy afterwards.
/// </para>
/// </remarks>
/// <param name="keyStore">The <see cref="IEncryptionKeyStorage"/>.</param>
/// <param name="cacheClient">The <see cref="IEncryptionKeyCacheClient"/> used to evict the key from every silo's cache.</param>
/// <param name="storage">The <see cref="IStorage"/> used to enumerate the event stores an erasure has to reach.</param>
public class PIIManager(IEncryptionKeyStorage keyStore, IEncryptionKeyCacheClient cacheClient, IStorage storage) : Grain, IPIIManager
{
    /// <inheritdoc/>
    public async Task DeleteEncryptionKeyFor(EncryptionKeyIdentifier identifier)
    {
        var key = GetKey();
        var eventStores = await EventStoresToReach(key);
        List<Exception> failures = [];

        // Fencing every event store before destroying anything in any of them is what closes the window the
        // per-store erasure could not: between the first delete and the last, one store still held the key and
        // another did not, and an event forwarded in that interval copied the survivor into a store that had just
        // been cleared. With the fence recorded first there is no interval in which any store accepts the key.
        await ForEach(eventStores, failures, eventStore => keyStore.RecordErasureFor(eventStore, key.Namespace, identifier));
        await ForEach(eventStores, failures, eventStore => keyStore.DeleteFor(eventStore, key.Namespace, identifier));

        // The eviction is not conditional on the erase succeeding. A composite key store attempts every store and
        // then reports a partial failure, so the key can be durably destroyed and the call still fail - and a
        // failure that skipped the eviction would leave every peer silo serving the erased key from a cache that
        // has no time-to-live and nothing else to clear it. Evicting is idempotent and cheap, so it always runs.
        await ForEach(eventStores, failures, eventStore => cacheClient.Evict(eventStore, key.Namespace, identifier));

        if (failures.Count > 0)
        {
            throw new EncryptionKeyErasureIncomplete(identifier, failures);
        }
    }

    /// <inheritdoc/>
    public async Task AllowNewEncryptionKeyFor(EncryptionKeyIdentifier identifier)
    {
        var key = GetKey();
        var eventStores = await EventStoresToReach(key);
        List<Exception> failures = [];

        await ForEach(eventStores, failures, eventStore => keyStore.AllowNewKeyFor(eventStore, key.Namespace, identifier));

        // A silo that remembers the subject as absent would keep answering from that memory instead of reaching the
        // store where the authorization now sits, so the next append would still fail to provision.
        await ForEach(eventStores, failures, eventStore => cacheClient.Evict(eventStore, key.Namespace, identifier));

        if (failures.Count > 0)
        {
            throw new EncryptionKeyLifecycleIncomplete(identifier, failures);
        }
    }

    static async Task ForEach(IEnumerable<EventStoreName> eventStores, List<Exception> failures, Func<EventStoreName, Task> operation)
    {
        foreach (var eventStore in eventStores)
        {
            try
            {
                await operation(eventStore);
            }
            catch (Exception error)
            {
                // Every event store is attempted even after one fails. Stopping at the first would leave the key
                // alive in a store that was never reached, and the caller sees the same failure either way.
                failures.Add(error);
            }
        }
    }

    async Task<IReadOnlyList<EventStoreName>> EventStoresToReach(PIIManagerKey key)
    {
        var eventStores = await storage.GetEventStores();

        // The event store this manager is addressed at is always included, even when the cluster listing does not
        // have it yet - an erasure that skipped the store the caller named would be the most surprising outcome of
        // all, and the listing is registration state rather than a statement about which keys exist.
        return [.. eventStores.Append(key.EventStore).Distinct()];
    }

    PIIManagerKey GetKey()
    {
        _ = this.GetPrimaryKey(out var primaryKeyExtension);
        return (PIIManagerKey)primaryKeyExtension!;
    }
}
