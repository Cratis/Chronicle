// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Migrations.for_ProtectedEventTypeMigrations.when_migrating_plaintext;

public class and_the_protected_source_belongs_to_another_subject : given.a_token_protected_only_in_the_latest_generation
{
    const string OtherSubject = "another-subject";
    string[] _released;
    string[] _ciphertexts;

    async Task Because()
    {
        _migrated = await _sut.MigratePlaintext(Store, Namespace, _eventType, _plaintext, _protectedSource, OtherSubject, definition: _definition);
        _ciphertexts = [Generation(1)["token"].GetValue<string>(), Generation(2)["token"].GetValue<string>()];
        await _keys.DeleteFor(Store, Namespace, Subject);
        _released = [await ReleasedToken(1, OtherSubject), await ReleasedToken(2, OtherSubject)];
    }

    [Fact] void should_not_reuse_the_other_subjects_ciphertext() => _ciphertexts.All(value => value != _protectedContent["token"].GetValue<string>()).ShouldBeTrue();
    [Fact] void should_release_both_targets_with_their_own_subjects_key() => _released.All(value => value == Token).ShouldBeTrue();
    [Fact] void should_not_store_plaintext_in_either_target() => _ciphertexts.All(value => value != Token).ShouldBeTrue();
    [Fact] void should_keep_the_original_generation_protected() => StoredGeneration(3).ShouldNotContain(Token);
}
