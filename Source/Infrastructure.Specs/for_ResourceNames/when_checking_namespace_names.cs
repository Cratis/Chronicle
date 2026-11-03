// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_ResourceNames;

public class when_checking_namespace_names : Specification
{
    static readonly string[] _reservedCharacters = ["a+es+b", "a#b", "a~0123456789abcdef", "a$b", "a\0b", "a\tb", "a\u0085b"];
    static readonly string[] _valid = ["orders", "Ørsted-prod_1", "a.b", "a b", "a:b", "a@b", "System", "Default", "main", "e\u0301", "admin", "config", "[NotSet]"];

    ResourceNameViolation[] _characters;
    ResourceNameViolation[] _valid_results;
    ResourceNameViolation _es;
    ResourceNameViolation _upperEs;
    ResourceNameViolation _mixedEs;
    ResourceNameViolation _empty;
    ResourceNameViolation _whitespace;

    void Because()
    {
        _characters = [.. _reservedCharacters.Select(ResourceNames.ForNamespace)];
        _valid_results = [.. _valid.Select(ResourceNames.ForNamespace)];
        _es = ResourceNames.ForNamespace("es");
        _upperEs = ResourceNames.ForNamespace("ES");
        _mixedEs = ResourceNames.ForNamespace("Es");
        _empty = ResourceNames.ForNamespace(null);
        _whitespace = ResourceNames.ForNamespace(" ");
    }

    [Fact] void should_refuse_reserved_characters() => _characters.ShouldEachConformTo(_ => _ == ResourceNameViolation.ReservedCharacter);
    [Fact] void should_accept_valid_names() => _valid_results.ShouldEachConformTo(_ => _ == ResourceNameViolation.None);
    [Fact] void should_refuse_es() => _es.ShouldEqual(ResourceNameViolation.ReservedName);
    [Fact] void should_refuse_upper_case_es() => _upperEs.ShouldEqual(ResourceNameViolation.ReservedName);
    [Fact] void should_refuse_mixed_case_es() => _mixedEs.ShouldEqual(ResourceNameViolation.ReservedName);
    [Fact] void should_refuse_empty() => _empty.ShouldEqual(ResourceNameViolation.Empty);
    [Fact] void should_refuse_whitespace() => _whitespace.ShouldEqual(ResourceNameViolation.Empty);
}
