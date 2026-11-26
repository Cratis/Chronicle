// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Events.Constraints.V2;

public class when_scope_tokens_are_validated : Specification
{
    Exception?[] _invalid;
    UniqueConstraintStorageScope _valid;

    void Because()
    {
        _invalid = new[] { "", "plaintext", new string('a', 31), new string('a', 33), new string('A', 32), new string('g', 32), new string(' ', 32) }
            .Select(text => Catch.Exception(() => _ = new UniqueConstraintStorageScope(text))).ToArray();
        _valid = new("0123456789abcdef0123456789abcdef");
    }

    [Fact] void should_require_an_explicit_32_character_lowercase_hex_token() => _invalid.All(_ => _ is InvalidUniqueConstraintV2Input).ShouldBeTrue();
    [Fact] void should_accept_an_encoded_scope_without_reinterpreting_it() => _valid.Value.ShouldEqual("0123456789abcdef0123456789abcdef");
    [Fact] void should_not_expose_scope_contents_in_errors() => _invalid.Select(_ => _?.Message).Distinct().Count().ShouldEqual(1);
}
