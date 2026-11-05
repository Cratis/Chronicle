// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Vault.for_VaultPathSegments.when_deciding_on_legacy_directories;

public class and_the_address_is_not_eligible : given.segments
{
    static readonly string[] _identifiers = ["a/b", "a%b", "a\\b", "a?b", "a#b", "a b", "a\tb", "..", ".", "a.", "a\u0000", "\uFF0F", "\u00D8/x", "\u00D8.", "\u00D8 x", "\u00D8\uD800", string.Empty];

    List<string> _eligible = [];

    void Because() => _eligible = [.. _identifiers.Where(identifier => VaultPathSegments.LegacyDirectory("store", "default", identifier) is not null)];

    [Fact] void should_not_use_an_earlier_directory_for_any_of_them() => _eligible.ShouldBeEmpty();
}
