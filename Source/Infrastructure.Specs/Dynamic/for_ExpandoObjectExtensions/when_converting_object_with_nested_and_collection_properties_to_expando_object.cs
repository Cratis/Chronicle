// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Dynamic.for_ExpandoObjectExtensions;

public class when_converting_object_with_nested_and_collection_properties_to_expando_object : Specification
{
    Guid _guid;
    IDictionary<string, object?> _result;

    void Establish() => _guid = Guid.NewGuid();

    void Because() => _result = new WithNestedProperties(
        "title",
        new NestedChild("one"),
        [new NestedChild("two"), new NestedChild("three")],
        _guid,
        new[] { 1, 2 },
        null).AsExpandoObject(camelCaseProperties: true);

    [Fact] void should_camel_case_property_names() => _result.Keys.ShouldContainOnly("title", "child", "children", "id", "numbers", "nothing");
    [Fact] void should_keep_string() => _result["title"].ShouldEqual("title");
    [Fact] void should_keep_null() => _result["nothing"].ShouldBeNull();
    [Fact] void should_keep_guid() => _result["id"].ShouldEqual(_guid);
    [Fact] void should_convert_nested_object_to_expando() => _result["child"].ShouldBeOfExactType<ExpandoObject>();
    [Fact] void should_camel_case_nested_property_names() => ((IDictionary<string, object?>)_result["child"]!)["childName"].ShouldEqual("one");
    [Fact] void should_convert_enumerable_to_object_array() => _result["children"].ShouldBeOfExactType<object[]>();
    [Fact] void should_convert_enumerable_elements_to_expando() => ((object[])_result["children"]!).All(_ => _ is ExpandoObject).ShouldBeTrue();
    [Fact] void should_keep_enumerable_count() => ((object[])_result["children"]!).Length.ShouldEqual(2);
    [Fact] void should_convert_primitive_array_to_object_array() => _result["numbers"].ShouldBeOfExactType<object[]>();
    [Fact] void should_keep_primitive_array_elements() => ((object[])_result["numbers"]!).ShouldContainOnly(1, 2);
}
