// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.EventSequences.Mutations;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.TestKit;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIManager;

public class when_addressing_the_mutation_digest_secret_through_the_compliance_surface : Specification
{
    static readonly EventStoreName _eventStore = "event-store";
    static readonly EncryptionKey _probe = new([9], [9]);

    static readonly EncryptionKeyIdentifier[] _aliases =
    [
        "chronicle-mutation-receipt-digest-v2",
        "CHRONICLE-MUTATION-RECEIPT-DIGEST-V2",
        "$chronicle-mutation-receipt-digest$v2$",
        "$CHRONICLE-Mutation-Receipt-Digest$V2$",
        "mutation-receipt-digest-v2",
        "chronicle-internal--event-store--mutation-receipt-digest-v2",
        "internal-secrets"
    ];

    static readonly EventStoreNamespaceName[] _namespaces =
    [
        EventStoreNamespaceName.Default,
        "internal",
        "chronicle-internal",
        "mutation-receipt-digest-v2"
    ];

    InMemoryEncryptionKeyStorage _keyStore;
    EncryptionKey _before;
    EncryptionKey _after;
    EventSequenceMutationDigestKey? _digestKeyAfter;

    async Task Establish()
    {
        _keyStore = new();
        await DigestKeys().TryGetFor(_eventStore);
        _before = await StoredSecret();
    }

    async Task Because()
    {
        foreach (var @namespace in _namespaces)
        {
            var manager = await ManagerFor(@namespace);
            foreach (var alias in _aliases)
            {
                // Provisioning on append ends in the key store's create-if-absent; erasure and re-authorization go
                // through the manager, the one entry point that accepts a caller-supplied identifier.
                await _keyStore.GetOrAddFor(_eventStore, @namespace, alias, new([7], [7]));
                await Attempt(() => manager.DeleteEncryptionKeyFor(alias));
                await Attempt(() => manager.AllowNewEncryptionKeyFor(alias));
                await _keyStore.GetOrAddFor(_eventStore, @namespace, alias, new([8], [8]));
            }
        }

        // A fresh provider has no cache, so it reads the secret from the key store again.
        _digestKeyAfter = await DigestKeys().TryGetFor(_eventStore);
        _after = await StoredSecret();
    }

    [Fact] void should_have_created_the_digest_secret() => ReferenceEquals(_before, _probe).ShouldBeFalse();
    [Fact] void should_keep_the_digest_secret_so_receipts_still_verify() => _after.Private.SequenceEqual(_before.Private).ShouldBeTrue();
    [Fact] void should_still_have_a_digest_key() => _digestKeyAfter.ShouldNotBeNull();

    static async Task Attempt(Func<Task> operation)
    {
        try
        {
            await operation();
        }
        catch (Exception ex) when (ex is EncryptionKeyErasureIncomplete or EncryptionKeyLifecycleIncomplete)
        {
            // Unique-claim removal runs against substituted storage; the key store operations have already run.
        }
    }

    async Task<PIIManager> ManagerFor(EventStoreNamespaceName @namespace)
    {
        // The test kit allows one grain per silo, so each namespace gets its own silo over the same key store.
        var silo = new TestKitSilo();
        var storage = Substitute.For<IStorage>();
        storage.GetEventStores().Returns([_eventStore]);
        silo.AddService<IEncryptionKeyStorage>(_keyStore);
        silo.AddService(Substitute.For<IEncryptionKeyCacheClient>());
        silo.AddService(storage);
        silo.AddService<Microsoft.Extensions.Logging.ILogger<PIIManager>>(NullLogger<PIIManager>.Instance);
        return await silo.CreateGrainAsync<PIIManager>(Guid.Empty, new PIIManagerKey(_eventStore, @namespace));
    }

    Task<EncryptionKey> StoredSecret() => _keyStore.GetOrAddInternalSecretFor(_eventStore, InternalSecretPurpose.MutationReceiptDigest, _probe);

    EventSequenceMutationDigestKeys DigestKeys() => new(_keyStore, NullLogger<EventSequenceMutationDigestKeys>.Instance);
}
