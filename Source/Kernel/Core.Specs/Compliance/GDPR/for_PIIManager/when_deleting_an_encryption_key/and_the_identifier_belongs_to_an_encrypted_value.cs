// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ProtectedValues;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIManager.when_deleting_an_encryption_key;

/// <summary>
/// Defense in depth for the one entry point that accepts an arbitrary caller-supplied identifier rather than
/// deriving one from a subject: the compliance gRPC surface. A key protecting a plain-confidentiality
/// [Encrypted] value has no data subject and no lawful basis for erasure, so this refuses before any storage
/// call - not after, and not with the erasure recorded and the delete skipped.
/// </summary>
public class and_the_identifier_belongs_to_an_encrypted_value : given.a_pii_manager
{
    static readonly EncryptionKeyIdentifier _encryptedValueIdentifier = EncryptedValueKeyIdentifiers.ForSubject(Identifier);

    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => _manager.DeleteEncryptionKeyFor(_encryptedValueIdentifier));

    [Fact] void should_throw_encryption_key_is_not_erasable() => _error.ShouldBeOfExactType<EncryptionKeyIsNotErasable>();
    [Fact] void should_not_record_an_erasure() => _keyStore.DidNotReceive().RecordErasureFor(Arg.Any<Concepts.EventStoreName>(), Arg.Any<Concepts.EventStoreNamespaceName>(), _encryptedValueIdentifier);
    [Fact] void should_not_delete_the_key() => _keyStore.DidNotReceive().DeleteFor(Arg.Any<Concepts.EventStoreName>(), Arg.Any<Concepts.EventStoreNamespaceName>(), _encryptedValueIdentifier);
    [Fact] void should_not_evict_any_cache() => _cacheClient.DidNotReceive().Evict(Arg.Any<Concepts.EventStoreName>(), Arg.Any<Concepts.EventStoreNamespaceName>(), _encryptedValueIdentifier);
}
