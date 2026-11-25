// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;

namespace Cratis.Chronicle.Dynamic.for_ExpandoObjectExtensions;

public class when_cloning_expando_object_with_collections_nested_in_collections : Specification
{
    ExpandoObject _original;
    IDictionary<string, object?> _clone;
    ExpandoObject _condition;

    void Establish()
    {
        _condition = new();
        dynamic condition = _condition;
        condition.property = "increase";
        condition.value = true;

        _original = new();
        dynamic asDynamic = _original;
        asDynamic.when = new object?[] { new object?[] { _condition } };
        asDynamic.byKey = new Dictionary<string, object> { ["rows"] = new object?[] { new object?[] { 42d } } };
    }

    void Because() => _clone = _original.Clone();

    IList InnerList() => (IList)((IList)_clone["when"]!)[0]!;
    IDictionary<string, object?> ClonedCondition() => (IDictionary<string, object?>)InnerList()[0]!;

    [Fact] void should_keep_the_inner_collection_elements_as_expando_objects() => InnerList()[0].ShouldBeOfExactType<ExpandoObject>();
    [Fact] void should_keep_the_boolean_value() => ClonedCondition()["value"].ShouldEqual(true);
    [Fact] void should_keep_the_string_value() => ClonedCondition()["property"].ShouldEqual("increase");
    [Fact] void should_not_share_the_inner_expando_object() => ReferenceEquals(InnerList()[0], _condition).ShouldBeFalse();
    [Fact] void should_keep_values_of_collections_nested_in_a_dictionary() => ((IList)((IList)((IDictionary)_clone["byKey"]!)["rows"]!)[0]!)[0].ShouldEqual(42d);
}
