// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Dynamic.for_ExpandoObjectExtensions;

public class when_finding_items_and_removing_nulls : Specification
{
    ExpandoObject _first;
    ExpandoObject _second;
    ExpandoObject _withoutIdentity;
    ExpandoObject _target;
    ExpandoObject _nested;
    List<ExpandoObject> _items;
    bool _containsFirst;
    bool _containsMissing;
    ExpandoObject? _found;
    ExpandoObject? _notFound;

    void Establish()
    {
        _first = Expando("id", "A");
        _second = Expando("id", "B");
        _withoutIdentity = Expando("other", 1);
        _items = [_withoutIdentity, _first, _second];

        _nested = Expando("keep", "x");
        ((IDictionary<string, object?>)_nested)["gone"] = null;
        _target = Expando("top", null);
        ((IDictionary<string, object?>)_target)["nested"] = _nested;
        ((IDictionary<string, object?>)_target)["value"] = 1;
    }

    void Because()
    {
        _containsFirst = _items.Contains("id", "A");
        _containsMissing = _items.Contains("id", "Z");
        _found = _items.GetItem("id", "B");
        _notFound = _items.GetItem("id", "Z");
        _target.RemoveNulls();
    }

    [Fact] void should_contain_existing_identity() => _containsFirst.ShouldBeTrue();
    [Fact] void should_not_contain_missing_identity() => _containsMissing.ShouldBeFalse();
    [Fact] void should_get_matching_item() => _found.ShouldEqual(_second);
    [Fact] void should_get_null_when_not_found() => _notFound.ShouldBeNull();
    [Fact] void should_remove_top_level_nulls() => ((IDictionary<string, object?>)_target).ContainsKey("top").ShouldBeFalse();
    [Fact] void should_remove_nested_nulls() => ((IDictionary<string, object?>)_nested).ContainsKey("gone").ShouldBeFalse();
    [Fact] void should_keep_values() => ((IDictionary<string, object?>)_target)["value"].ShouldEqual(1);
    [Fact] void should_keep_nested_values() => ((IDictionary<string, object?>)_nested)["keep"].ShouldEqual("x");

    static ExpandoObject Expando(string key, object? value)
    {
        var expando = new ExpandoObject();
        ((IDictionary<string, object?>)expando)[key] = value;
        return expando;
    }
}
