// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Migrations.for_ProtectedEventTypeMigrations.when_migrating_plaintext;

public class and_lower_generations_have_no_metadata : given.a_token_protected_only_in_the_latest_generation
{
    string[] _released;
    string[] _ciphertexts;

    async Task Because()
    {
        _migrated = await _sut.MigratePlaintext(Store, Namespace, _eventType, _plaintext, _protectedSource, Subject, definition: _definition);
        _ciphertexts = [Generation(1)["token"]!.GetValue<string>(), Generation(2)["token"]!.GetValue<string>()];
        _released = [await ReleasedToken(1, Subject), await ReleasedToken(2, Subject)];
    }

    [Fact] void should_not_store_the_token_as_plaintext_in_the_first_generation() => StoredGeneration(1).ShouldNotContain(Token);
    [Fact] void should_not_store_the_token_as_plaintext_in_the_second_generation() => StoredGeneration(2).ShouldNotContain(Token);
    [Fact] void should_keep_the_appended_generation_as_protected() => _migrated[(Concepts.Events.EventTypeGeneration)3].ShouldBeSame(_protectedSource);
    [Fact] void should_encrypt_afresh_when_the_source_envelope_cannot_identify_its_key_revision() => _ciphertexts.All(value => value != _protectedContent["token"]!.GetValue<string>()).ShouldBeTrue();
    [Fact] void should_release_both_unchanged_target_values() => _released.All(value => value == Token).ShouldBeTrue();
}
