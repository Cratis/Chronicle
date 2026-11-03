// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_ResourceNames;

public class when_checking_event_store_names : Specification
{
    static readonly string[] _reservedCharacters = ["a+es+b", "a#b", "a~0123456789abcdef", "a$b", "a\0b", "a\tb", "a\u0085b", "a\u007Fb"];
    static readonly string[] _reservedNames = ["es", "ES", "Es", "admin", "Admin", "Local", "LOCAL", "config", "Config"];
    static readonly string[] _valid = ["orders", "Ørsted-prod_1", "a.b", "a b", "a:b", "a@b", "System", "Default", "e\u0301", "chronicle", "main", "escape", "adminx"];

    ResourceNameViolation[] _characters;
    ResourceNameViolation[] _names;
    ResourceNameViolation[] _valid_results;
    ResourceNameViolation _null;
    ResourceNameViolation _empty;
    ResourceNameViolation _whitespace;
    ResourceNameViolation _notSet;
    ResourceNameViolation _notSetDifferentCase;

    void Because()
    {
        _characters = [.. _reservedCharacters.Select(ResourceNames.ForEventStore)];
        _names = [.. _reservedNames.Select(ResourceNames.ForEventStore)];
        _valid_results = [.. _valid.Select(ResourceNames.ForEventStore)];
        _null = ResourceNames.ForEventStore(null);
        _empty = ResourceNames.ForEventStore(string.Empty);
        _whitespace = ResourceNames.ForEventStore("  ");
        _notSet = ResourceNames.ForEventStore("[NotSet]");
        _notSetDifferentCase = ResourceNames.ForEventStore("[notset]");
    }

    [Fact] void should_refuse_reserved_characters() => _characters.ShouldEachConformTo(_ => _ == ResourceNameViolation.ReservedCharacter);
    [Fact] void should_refuse_reserved_names() => _names.ShouldEachConformTo(_ => _ == ResourceNameViolation.ReservedName);
    [Fact] void should_accept_valid_names() => _valid_results.ShouldEachConformTo(_ => _ == ResourceNameViolation.None);
    [Fact] void should_refuse_null() => _null.ShouldEqual(ResourceNameViolation.Empty);
    [Fact] void should_refuse_empty() => _empty.ShouldEqual(ResourceNameViolation.Empty);
    [Fact] void should_refuse_whitespace() => _whitespace.ShouldEqual(ResourceNameViolation.Empty);
    [Fact] void should_refuse_not_set() => _notSet.ShouldEqual(ResourceNameViolation.NotSet);
    [Fact] void should_refuse_not_set_in_other_case() => _notSetDifferentCase.ShouldEqual(ResourceNameViolation.NotSet);
}
