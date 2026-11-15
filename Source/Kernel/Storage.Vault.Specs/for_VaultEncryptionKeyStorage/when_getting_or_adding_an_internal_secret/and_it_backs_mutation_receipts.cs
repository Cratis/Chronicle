// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.EventSequences.Mutations;
using Microsoft.Extensions.Logging.Abstractions;
using VaultSharp;

namespace Cratis.Chronicle.Storage.Vault.for_VaultEncryptionKeyStorage.when_getting_or_adding_an_internal_secret;

/// <summary>
/// Vault cannot hold internal secrets yet, so a Vault-backed key store - on its own or as the primary of a composite -
/// refuses, and mutation receipts fail closed instead of being keyed from a secret kept anywhere else.
/// </summary>
public class and_it_backs_mutation_receipts : Specification
{
    static readonly EncryptionKey _probe = new([9], [9]);

    IVaultClient _client;
    InMemoryEncryptionKeyStorage _defaultStorage;
    Exception _direct;
    Exception _throughComposite;
    EventSequenceMutationDigestKey? _digestKey;
    EncryptionKey _inDefaultStorage;

    void Establish()
    {
        _client = Substitute.For<IVaultClient>();
        _defaultStorage = new();
    }

    async Task Because()
    {
        var vault = new VaultEncryptionKeyStorage(_client);
        var composite = new CompositeEncryptionKeyStorage(vault, _defaultStorage);
        _direct = await Catch.Exception(() => ((IEncryptionKeyStorage)vault).GetOrAddInternalSecretFor("event-store", InternalSecretPurpose.MutationReceiptDigest, new([1], [2])));
        _throughComposite = await Catch.Exception(() => composite.GetOrAddInternalSecretFor("event-store", InternalSecretPurpose.MutationReceiptDigest, new([1], [2])));
        _digestKey = await new EventSequenceMutationDigestKeys(composite, NullLogger<EventSequenceMutationDigestKeys>.Instance).TryGetFor("event-store");
        _inDefaultStorage = await _defaultStorage.GetOrAddInternalSecretFor("event-store", InternalSecretPurpose.MutationReceiptDigest, _probe);
    }

    [Fact] void should_refuse_directly() => _direct.ShouldBeOfExactType<InternalSecretsNotSupported>();
    [Fact] void should_refuse_through_the_composite() => _throughComposite.ShouldBeOfExactType<InternalSecretsNotSupported>();
    [Fact] void should_have_no_digest_key() => _digestKey.ShouldBeNull();
    [Fact] void should_not_fall_back_to_the_default_storage() => ReferenceEquals(_inDefaultStorage, _probe).ShouldBeTrue();
    [Fact] void should_not_touch_vault() => _client.ReceivedCalls().ShouldBeEmpty();
}
