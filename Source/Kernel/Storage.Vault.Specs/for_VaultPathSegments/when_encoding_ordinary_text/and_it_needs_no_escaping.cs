// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Vault.for_VaultPathSegments.when_encoding_ordinary_text;

public class and_it_needs_no_escaping : given.segments
{
    static readonly string[] _values =
    [
        "3f2a9c1e-7b4d-4e58-9a61-0c2d5e8f1a37",
        "customer-42",
        "Default",
        "john.doe+tag@example.com",
        "$chronicle-encrypted-value$subject$x",
        "a=b,c:d"
    ];

    List<string> _changed = [];
    List<string> _copied = [];

    void Because()
    {
        _changed = [.. _values.Where(value => Encode(value) != value)];
        _copied = [.. _values.Where(value => !ReferenceEquals(Encode(value), value))];
    }

    [Fact] void should_leave_the_text_unchanged() => _changed.ShouldBeEmpty();
    [Fact] void should_return_the_same_string_instance() => _copied.ShouldBeEmpty();
}
