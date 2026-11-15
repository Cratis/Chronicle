// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ProtectedValues;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_SecretNames.when_deciding_on_legacy_names;

public class and_the_identifier_is_not_eligible : given.key_names
{
    static readonly string[] _identifiers =
    [
        "Alice.X",
        "alice_x",
        "ALICE",
        "a--b",
        "-a",
        "a-",
        "john.doe@example.com",
        "chronicle-encrypted-value-subject-x"
    ];

    List<string> _eligible = [];
    string? _subjectWithPunctuation;

    void Because()
    {
        _eligible = [.. _identifiers.Where(identifier => LegacyOf("store", "default", identifier) is not null)];
        _subjectWithPunctuation = LegacyOf("store", "default", EncryptedValueKeyIdentifiers.ForSubject("Alice.X").Value);
    }

    [Fact] void should_not_offer_an_earlier_name_for_any_of_them() => _eligible.ShouldBeEmpty();
    [Fact] void should_not_offer_an_earlier_name_for_a_subject_that_is_not_canonical() => _subjectWithPunctuation.ShouldBeNull();
}
