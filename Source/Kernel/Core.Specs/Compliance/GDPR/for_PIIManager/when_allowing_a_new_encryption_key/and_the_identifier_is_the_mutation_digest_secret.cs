// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Storage.EventSequences.Mutations;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIIManager.when_allowing_a_new_encryption_key;

/// <summary>
/// The same refusal as deletion - see the sibling spec under when_deleting_an_encryption_key.
/// </summary>
public class and_the_identifier_is_the_mutation_digest_secret : given.a_pii_manager
{
    static readonly EncryptionKeyIdentifier _secret = EventSequenceMutationDigestKeyIdentifiers.Secret;

    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => _manager.AllowNewEncryptionKeyFor(_secret));

    [Fact] void should_throw_encryption_key_is_not_erasable() => _error.ShouldBeOfExactType<EncryptionKeyIsNotErasable>();
    [Fact] void should_not_authorize_a_new_key() => _keyStore.DidNotReceive().AllowNewKeyFor(Arg.Any<Concepts.EventStoreName>(), Arg.Any<Concepts.EventStoreNamespaceName>(), _secret);
}
