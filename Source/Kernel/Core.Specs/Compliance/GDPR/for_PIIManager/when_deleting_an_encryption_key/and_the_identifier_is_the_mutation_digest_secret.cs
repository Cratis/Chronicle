// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Storage.EventSequences.Mutations;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIManager.when_deleting_an_encryption_key;

/// <summary>
/// The secret behind mutation receipt digests holds no personal data; erasing it would only make every keyed receipt of
/// the event store unverifiable, so the compliance surface refuses it before any storage call.
/// </summary>
public class and_the_identifier_is_the_mutation_digest_secret : given.a_pii_manager
{
    static readonly EncryptionKeyIdentifier _secret = EventSequenceMutationDigestKeyIdentifiers.Secret;

    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => _manager.DeleteEncryptionKeyFor(_secret));

    [Fact] void should_throw_encryption_key_is_not_erasable() => _error.ShouldBeOfExactType<EncryptionKeyIsNotErasable>();
    [Fact] void should_not_record_an_erasure() => _keyStore.DidNotReceive().RecordErasureFor(Arg.Any<Concepts.EventStoreName>(), Arg.Any<Concepts.EventStoreNamespaceName>(), _secret);
    [Fact] void should_not_delete_the_key() => _keyStore.DidNotReceive().DeleteFor(Arg.Any<Concepts.EventStoreName>(), Arg.Any<Concepts.EventStoreNamespaceName>(), _secret);
}
