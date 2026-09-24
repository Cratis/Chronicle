// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ProtectedValues;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIManager.when_allowing_a_new_encryption_key;

/// <summary>
/// The same defense-in-depth refusal as deletion - see the sibling spec under when_deleting_an_encryption_key.
/// An [Encrypted] value's key never goes through the erased/authorize-a-new-key lifecycle at all.
/// </summary>
public class and_the_identifier_belongs_to_an_encrypted_value : given.a_pii_manager
{
    static readonly EncryptionKeyIdentifier _encryptedValueIdentifier = EncryptedValueKeyIdentifiers.ForSubject(Identifier);

    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => _manager.AllowNewEncryptionKeyFor(_encryptedValueIdentifier));

    [Fact] void should_throw_encryption_key_is_not_erasable() => _error.ShouldBeOfExactType<EncryptionKeyIsNotErasable>();
    [Fact] void should_not_authorize_a_new_key() => _keyStore.DidNotReceive().AllowNewKeyFor(Arg.Any<Concepts.EventStoreName>(), Arg.Any<Concepts.EventStoreNamespaceName>(), _encryptedValueIdentifier);
    [Fact] void should_not_evict_any_cache() => _cacheClient.DidNotReceive().Evict(Arg.Any<Concepts.EventStoreName>(), Arg.Any<Concepts.EventStoreNamespaceName>(), _encryptedValueIdentifier);
}
