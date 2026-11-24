// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_JsonColumnSearch.when_comparing;

public class and_the_key_is_composite : given.composite_children
{
    bool _matching;
    bool _matchesTenantOnly;
    bool _matchesTargetOnly;
    bool _dictionary;
    bool _jsonElement;
    bool _jsonObject;
    bool _typeName;

    void Because()
    {
        _matching = JsonColumnSearch.CompareLoose(Key(), Key());
        _matchesTenantOnly = JsonColumnSearch.CompareLoose(Key(), Key("tenant", "other"));
        _matchesTargetOnly = JsonColumnSearch.CompareLoose(Key(), Key("other", "target"));
        _dictionary = JsonColumnSearch.CompareLoose(Key(), new Dictionary<string, object?> { ["target"] = "target", ["tenant"] = "tenant" });
        _jsonElement = JsonColumnSearch.CompareLoose(Key(), JsonSerializer.SerializeToElement(Key()));
        _jsonObject = JsonColumnSearch.CompareLoose(Key(), JsonSerializer.SerializeToNode(Key()));
        _typeName = JsonColumnSearch.CompareLoose(Key(), typeof(System.Dynamic.ExpandoObject).FullName);
    }

    [Fact] void should_match_a_fresh_composite_key() => _matching.ShouldBeTrue();
    [Fact] void should_not_match_only_the_tenant() => _matchesTenantOnly.ShouldBeFalse();
    [Fact] void should_not_match_only_the_target() => _matchesTargetOnly.ShouldBeFalse();
    [Fact] void should_match_a_dictionary_regardless_of_property_order() => _dictionary.ShouldBeTrue();
    [Fact] void should_match_a_json_element() => _jsonElement.ShouldBeTrue();
    [Fact] void should_match_a_json_object() => _jsonObject.ShouldBeTrue();
    [Fact] void should_not_match_the_type_name() => _typeName.ShouldBeFalse();
}
