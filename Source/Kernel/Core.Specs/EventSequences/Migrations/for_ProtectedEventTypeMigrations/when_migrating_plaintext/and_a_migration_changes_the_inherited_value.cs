// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.EventSequences.Migrations.for_ProtectedEventTypeMigrations.when_migrating_plaintext;

public class and_a_migration_changes_the_inherited_value : given.a_token_protected_only_in_the_latest_generation
{
    const string TransformedToken = "transformed-personal-token";
    string[] _released;
    string[] _ciphertexts;

    void Establish() => _definition = _definition with
    {
        Migrations = [
            new(1, 2, [], new JsonObject(), new JsonObject()),
            new(2, 3, [], new JsonObject(), new JsonObject { ["token"] = new JsonObject { ["$defaultValue"] = TransformedToken } })
        ]
    };

    async Task Because()
    {
        _migrated = await _sut.MigratePlaintext(Store, Namespace, _eventType, _plaintext, _protectedSource, Subject, definition: _definition);
        _ciphertexts = [Generation(1)["token"]!.GetValue<string>(), Generation(2)["token"]!.GetValue<string>()];
        _released = [await ReleasedToken(1, Subject), await ReleasedToken(2, Subject)];
    }

    [Fact] void should_encrypt_each_transformed_value_instead_of_copying_the_source() => _ciphertexts.All(value => value != _protectedContent["token"]!.GetValue<string>()).ShouldBeTrue();
    [Fact] void should_release_both_transformed_values() => _released.All(value => value == TransformedToken).ShouldBeTrue();
    [Fact] void should_not_store_the_transformed_plaintext() => _ciphertexts.All(value => value != TransformedToken).ShouldBeTrue();
    [Fact] void should_keep_the_original_generation_protected() => StoredGeneration(3).ShouldNotContain(Token);
}
