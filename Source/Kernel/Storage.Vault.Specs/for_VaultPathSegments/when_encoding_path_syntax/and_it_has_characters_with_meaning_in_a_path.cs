// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Vault.for_VaultPathSegments.when_encoding_path_syntax;

public class and_it_has_characters_with_meaning_in_a_path : given.segments
{
    static readonly string[] _values =
    [
        "S/ns/../../X/p#", "..", ".", "a/../b", "a\\b", "a%2Fb", "%2e%2e", "a?b", "a#b", "a.", "a b", "\u0000", "\n", "\uFF0F", "~2F"
    ];

    List<string> _unsafe = [];

    void Because() => _unsafe = [.. _values.Where(value => !IsSafe(Encode(value)))];

    [Fact] void should_leave_no_separator_escape_whitespace_or_control_character() => _unsafe.ShouldBeEmpty();

    static bool IsSafe(string segment) =>
        segment is not ("." or "..") &&
        !segment.EndsWith('.') &&
        segment.All(character => character is not ('/' or '\\' or '?' or '#' or '%') && !char.IsWhiteSpace(character) && !char.IsControl(character) && character < 0x80);
}
