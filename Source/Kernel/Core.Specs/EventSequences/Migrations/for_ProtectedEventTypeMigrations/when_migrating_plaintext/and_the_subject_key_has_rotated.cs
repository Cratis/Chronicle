// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.EventSequences.Migrations.for_ProtectedEventTypeMigrations.when_migrating_plaintext;

public class and_the_subject_key_has_rotated : given.a_token_protected_only_in_the_latest_generation
{
    EncryptionKeyAtRevision _originalKey;
    EncryptionKeyAtRevision _rotatedKey;
    string[] _released;
    string[] _ciphertexts;

    async Task Establish()
    {
        _originalKey = await _keys.TryGetLatestFor(Store, Namespace, Subject);
        await _keys.SaveFor(Store, Namespace, Subject, new Encryption().GenerateKey());
        _rotatedKey = await _keys.TryGetLatestFor(Store, Namespace, Subject);
    }

    async Task Because()
    {
        _migrated = await _sut.MigratePlaintext(Store, Namespace, _eventType, _plaintext, _protectedSource, Subject, definition: _definition);
        _ciphertexts = [Generation(1)["token"].GetValue<string>(), Generation(2)["token"].GetValue<string>()];
        _released = [await ReleasedToken(1, Subject), await ReleasedToken(2, Subject)];
    }

    [Fact] void should_use_a_different_key_revision() => _rotatedKey.Revision.ShouldNotEqual(_originalKey.Revision);
    [Fact] void should_encrypt_both_targets_afresh() => _ciphertexts.All(value => value != _protectedContent["token"].GetValue<string>()).ShouldBeTrue();
    [Fact] void should_release_both_targets_with_the_rotated_key() => _released.All(value => value == Token).ShouldBeTrue();
    [Fact] void should_not_store_plaintext_in_either_target() => _ciphertexts.All(value => value != Token).ShouldBeTrue();
    [Fact] void should_keep_the_original_generation_protected() => StoredGeneration(3).ShouldNotContain(Token);
}
