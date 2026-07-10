// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIICompliancePropertyValueHandler.when_provisioning_a_key_concurrently.given;

/// <summary>
/// An <see cref="IEncryptionKeyStorage"/> that models a MongoDB replica set (or a second silo) where a
/// read can observe state older than the latest write: writes land on a "primary" but reads are served
/// from a "secondary" that only catches up when <see cref="LaggingReadKeyStorage.Replicate"/> is called. This is enough to
/// defeat the check-then-act provisioning in <c>EnsureKeyFor</c> — two provisioners for one subject can
/// both observe "no key" and each save a key, and the store then returns whichever was written last.
/// It delegates to a real <see cref="InMemoryEncryptionKeyStorage"/> for the actual revisioned behavior.
/// </summary>
public class a_key_store_with_read_after_write_lag : Specification
{
    protected LaggingReadKeyStorage _keyStore;

    void Establish() => _keyStore = new LaggingReadKeyStorage();

    /// <summary>
    /// An <see cref="IEncryptionKeyStorage"/> whose reads lag behind its writes until <see cref="Replicate"/>.
    /// </summary>
    public sealed class LaggingReadKeyStorage : IEncryptionKeyStorage
    {
        readonly InMemoryEncryptionKeyStorage _primary = new();
        readonly InMemoryEncryptionKeyStorage _secondary = new();
        readonly List<(EventStoreName EventStore, EventStoreNamespaceName Namespace, EncryptionKeyIdentifier Identifier, EncryptionKey Key, EncryptionKeyRevision? Revision)> _pending = [];

        public async Task SaveFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key, EncryptionKeyRevision? revision = null)
        {
            await _primary.SaveFor(eventStore, eventStoreNamespace, identifier, key, revision);
            _pending.Add((eventStore, eventStoreNamespace, identifier, key, revision));
        }

        public async Task<EncryptionKey> GetOrAddFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key)
        {
            // Writes (and the atomic get-or-create) land on the primary — the ordering point — even though
            // reads are served from a lagging secondary. This models MongoDB, where the insert-if-absent is
            // executed against the primary regardless of read preference.
            var result = await _primary.GetOrAddFor(eventStore, eventStoreNamespace, identifier, key);
            _pending.Add((eventStore, eventStoreNamespace, identifier, result, EncryptionKeyRevision.Initial));
            return result;
        }

        public Task<bool> HasFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null) =>
            _secondary.HasFor(eventStore, eventStoreNamespace, identifier, revision);

        public Task<EncryptionKey?> TryGetFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null) =>
            _secondary.TryGetFor(eventStore, eventStoreNamespace, identifier, revision);

        public Task<EncryptionKey> GetFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null) =>
            _secondary.GetFor(eventStore, eventStoreNamespace, identifier, revision);

        public Task DeleteFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null) =>
            Task.WhenAll(
                _primary.DeleteFor(eventStore, eventStoreNamespace, identifier, revision),
                _secondary.DeleteFor(eventStore, eventStoreNamespace, identifier, revision));

        /// <summary>Catches the secondary up to the primary, making prior writes visible to reads.</summary>
        /// <returns>Awaitable task.</returns>
        public async Task Replicate()
        {
            foreach (var (eventStore, eventStoreNamespace, identifier, key, revision) in _pending)
            {
                await _secondary.SaveFor(eventStore, eventStoreNamespace, identifier, key, revision);
            }

            _pending.Clear();
        }

        /// <summary>Counts how many distinct revisions exist on the primary for a subject.</summary>
        /// <param name="eventStore">The event store.</param>
        /// <param name="eventStoreNamespace">The namespace.</param>
        /// <param name="identifier">The subject identifier.</param>
        /// <returns>The number of persisted revisions.</returns>
        public async Task<int> RevisionCountOnPrimary(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
        {
            var count = 0;
            for (var revision = 1u; revision <= 16; revision++)
            {
                if (await _primary.HasFor(eventStore, eventStoreNamespace, identifier, new EncryptionKeyRevision(revision)))
                {
                    count++;
                }
            }

            return count;
        }
    }
}
