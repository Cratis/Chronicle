// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_SecretNames.when_naming_keys;

public class and_identifiers_differ_only_in_case_or_punctuation : given.key_names
{
    static readonly (string, string)[] _pairs =
    [
        ("Alice.X", "alice-x"),
        ("A", "a"),
        ("a_b", "a-b"),
        ("a--b", "a-b"),
        ("-a", "a"),
        ("a b", "a-b")
    ];

    List<(string, string)> _sharing = [];

    void Because() => _sharing = [.. _pairs.Where(pair => PrefixOf("store", "default", pair.Item1) == PrefixOf("store", "default", pair.Item2))];

    [Fact] void should_give_every_pair_different_prefixes() => _sharing.ShouldBeEmpty();
}
