// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ProtectedValues;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_SecretNames.when_naming_keys;

public class and_one_is_an_encrypted_value_identifier : given.key_names
{
    bool _subjectDiffers;
    bool _globalDiffers;

    void Because()
    {
        _subjectDiffers = Differ(["store", "default", EncryptedValueKeyIdentifiers.ForSubject("x").Value], ["store", "default", "chronicle-encrypted-value-subject-x"]);
        _globalDiffers = Differ(
            [EncryptedValueKeyIdentifiers.GlobalEventStore.Value, EncryptedValueKeyIdentifiers.GlobalNamespace.Value, EncryptedValueKeyIdentifiers.ForGlobal().Value],
            ["chronicle-encrypted-value-global-scope", "chronicle-encrypted-value-global-scope", "chronicle-encrypted-value-global"]);
    }

    [Fact] void should_tell_a_subject_identifier_from_its_plain_lookalike() => _subjectDiffers.ShouldBeTrue();
    [Fact] void should_tell_the_global_scope_from_its_plain_lookalike() => _globalDiffers.ShouldBeTrue();
}
