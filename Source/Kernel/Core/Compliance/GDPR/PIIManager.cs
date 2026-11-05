// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging;

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
/// <param name="logger">The <see cref="ILogger{TCategoryName}"/> the compliance acts are recorded to.</param>
public class PIIManager(
    IEncryptionKeyStorage keyStore,
    IEncryptionKeyCacheClient cacheClient,
    IStorage storage,
    ILogger<PIIManager> logger) : Grain, IPIIManager
{
    /// <inheritdoc/>
    public async Task DeleteEncryptionKeyFor(EncryptionKeyIdentifier identifier)
    {
        ThrowIfEncryptedValueIdentifier(identifier);
        var key = GetKey();
        List<Exception> failures = [];
        var eventStores = await EventStoresToReach(key, failures);
        await DeleteEncryptionKey(identifier, key, eventStores, failures);
    }

    /// <inheritdoc/>
    public Task DeleteEncryptionKeyIn(EncryptionKeyIdentifier identifier, EventStoreName[] eventStores)
    {
        ThrowIfEncryptedValueIdentifier(identifier);
        var key = GetKey();
        return DeleteEncryptionKey(identifier, key, AuthorizedStores(key, eventStores), []);
    }

    /// <inheritdoc/>
    public async Task AllowNewEncryptionKeyFor(EncryptionKeyIdentifier identifier)
    {
        ThrowIfEncryptedValueIdentifier(identifier);
        var key = GetKey();
        List<Exception> failures = [];
        var eventStores = await EventStoresToReach(key, failures);
        await AllowNewEncryptionKey(identifier, key, eventStores, failures);
    }

    /// <inheritdoc/>
    public Task AllowNewEncryptionKeyIn(EncryptionKeyIdentifier identifier, EventStoreName[] eventStores)
    {
        ThrowIfEncryptedValueIdentifier(identifier);
        var key = GetKey();
        return AllowNewEncryptionKey(identifier, key, AuthorizedStores(key, eventStores), []);
    }

    static void ThrowIfEncryptedValueIdentifier(EncryptionKeyIdentifier identifier)
    {
        // Defense in depth: the PII path never constructs an identifier carrying the reserved marker (see
        // EncryptedValueKeyIdentifiers), so this can only be reached when a caller on the compliance gRPC surface
        // - the one entry point that accepts an arbitrary caller-supplied identifier - addresses an erasure or
        // authorization at one directly.
        if (EncryptedValueKeyIdentifiers.IsEncryptedValueIdentifier(identifier))
        {
            throw new EncryptionKeyIsNotErasable(identifier);
        }
    }

    static string Names(IEnumerable<EventStoreName> eventStores) => string.Join(", ", eventStores.Select(_ => _.Value));

    static string BindingFor(EncryptionKeyIdentifier identifier)
    {
        // The act is logged, the person is not. Chronicle deliberately does not name the data subject in its logs -
        // a line naming an erased person is unencrypted personal data that outlives the crypto-shred, and unlike the
        // erasure fence, which lives inside the key store and is destroyed with it, a log line travels to
        // aggregators whose retention and access sit outside the deployment's data boundary. The binding is a
        // stable one-way derivation of the identifier, so an operator who already knows the subject can compute it
        // and find the act; it just does not hand the name to everyone who can read a log. It is a pseudonym rather
        // than an anonymization - a low-entropy identifier space is brute-forceable - which is exactly the trade a
        // pseudonymous binding is meant to make.
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(identifier.Value)));
    }

    static bool IsSystem(EventStoreName eventStore) => eventStore.Value.Equals(EventStoreName.System.Value, StringComparison.OrdinalIgnoreCase);

    static EventStoreName[] AuthorizedStores(PIIManagerKey key, IEnumerable<EventStoreName> eventStores) =>
        eventStores.Where(eventStore => !IsSystem(eventStore) || IsSystem(key.EventStore)).Distinct().ToArray();

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

    async Task DeleteEncryptionKey(EncryptionKeyIdentifier identifier, PIIManagerKey key, IReadOnlyList<EventStoreName> eventStores, List<Exception> failures)
    {
        // Fence all authorized stores before deleting any key, so a forwarded event cannot restore an erased key.
        await ForEach(eventStores, failures, eventStore => keyStore.RecordErasureFor(eventStore, key.Namespace, identifier));
        await ForEach(eventStores, failures, eventStore => keyStore.DeleteFor(eventStore, key.Namespace, identifier));
        await ForEach(eventStores, failures, eventStore => RemoveUniqueClaimsFor(eventStore, key.Namespace, identifier));

        // Evict even after a partial storage failure: a durable deletion must not leave a cached key alive.
        await ForEach(eventStores, failures, eventStore => cacheClient.Evict(eventStore, key.Namespace, identifier));
        if (failures.Count > 0)
        {
            logger.SubjectErasureIncomplete(BindingFor(identifier), key.Namespace, failures.Count, eventStores.Count, Names(eventStores));
            throw new EncryptionKeyErasureIncomplete(identifier, failures);
        }

        logger.ErasedSubject(BindingFor(identifier), key.Namespace, eventStores.Count, Names(eventStores));
    }

    async Task AllowNewEncryptionKey(EncryptionKeyIdentifier identifier, PIIManagerKey key, IReadOnlyList<EventStoreName> eventStores, List<Exception> failures)
    {
        await ForEach(eventStores, failures, eventStore => keyStore.AllowNewKeyFor(eventStore, key.Namespace, identifier));

        // A cached absence must not hide the authorization to provision a fresh key.
        await ForEach(eventStores, failures, eventStore => cacheClient.Evict(eventStore, key.Namespace, identifier));
        if (failures.Count > 0)
        {
            logger.NewEncryptionKeyAuthorizationIncomplete(BindingFor(identifier), key.Namespace, failures.Count, eventStores.Count, Names(eventStores));
            throw new EncryptionKeyLifecycleIncomplete(identifier, failures);
        }

        logger.AllowedNewEncryptionKey(BindingFor(identifier), key.Namespace, eventStores.Count, Names(eventStores));
    }

    async Task<IReadOnlyList<EventStoreName>> EventStoresToReach(PIIManagerKey key, List<Exception> failures)
    {
        if (IsSystem(key.EventStore)) return [key.EventStore];
        try
        {
            // The event store this manager is addressed at is always included, even when the cluster listing does
            // not have it yet - an erasure that skipped the store the caller named would be the most surprising
            // outcome of all, and the listing is registration state rather than a statement about which keys exist.
            var eventStores = await storage.GetEventStores();
            return [.. eventStores.Append(key.EventStore).Where(eventStore => !IsSystem(eventStore)).Distinct()];
        }
        catch (Exception error)
        {
            // Losing the listing must not lose the erasure. The event store the caller named is erased either way,
            // and the failure is reported so a partial reach is never mistaken for a complete one.
            failures.Add(error);
            return [key.EventStore];
        }
    }

    async Task RemoveUniqueClaimsFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, EncryptionKeyIdentifier identifier)
    {
        var namespaceStorage = storage.GetEventStore(eventStore).GetNamespace(@namespace);
        var subject = new Subject(identifier.Value);
        foreach (var eventSequence in await namespaceStorage.GetEventSequences())
        {
            await namespaceStorage.GetUniqueConstraintsStorage(eventSequence).RemoveForSubject(subject);
        }
    }

    PIIManagerKey GetKey()
    {
        _ = this.GetPrimaryKey(out var primaryKeyExtension);
        return (PIIManagerKey)primaryKeyExtension!;
    }
}
