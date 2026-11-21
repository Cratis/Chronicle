// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Dynamic.for_ExpandoObjectExtensions;

public class when_cloning_expando_object_with_nested_and_collection_values : Specification
{
    ExpandoObject _original;
    ExpandoObject _nested;
    ExpandoObject _listChild;
    IDictionary<string, object?> _clone;
    Guid _guid;
    byte[] _bytes;

    void Establish()
    {
        _guid = Guid.NewGuid();
        _bytes = [1, 2, 3];
        _nested = new ExpandoObject();
        ((IDictionary<string, object?>)_nested)["inner"] = "value";
        _listChild = new ExpandoObject();
        ((IDictionary<string, object?>)_listChild)["name"] = "child";

        _original = new ExpandoObject();
        var dictionary = (IDictionary<string, object?>)_original;
        dictionary["Nothing"] = null;
        dictionary["Nested"] = _nested;
        dictionary["Children"] = new List<ExpandoObject> { _listChild };
        dictionary["Numbers"] = new List<int> { 1, 2, 3 };
        dictionary["Text"] = "text";
        dictionary["Id"] = _guid;
        dictionary["Bytes"] = _bytes;
        dictionary["Number"] = 42;
    }

    void Because() => _clone = _original.Clone();

    [Fact] void should_keep_null_values() => _clone["Nothing"].ShouldBeNull();
    [Fact] void should_clone_nested_expando() => ReferenceEquals(_clone["Nested"], _nested).ShouldBeFalse();
    [Fact] void should_keep_nested_expando_content() => ((IDictionary<string, object?>)_clone["Nested"]!)["inner"].ShouldEqual("value");
    [Fact] void should_convert_collection_to_list_of_same_element_type() => _clone["Children"].ShouldBeOfExactType<List<ExpandoObject>>();
    [Fact] void should_clone_collection_elements() => ReferenceEquals(((List<ExpandoObject>)_clone["Children"]!)[0], _listChild).ShouldBeFalse();
    [Fact] void should_keep_collection_element_content() => ((IDictionary<string, object?>)((List<ExpandoObject>)_clone["Children"]!)[0])["name"].ShouldEqual("child");
    [Fact] void should_convert_primitive_collection_to_list() => _clone["Numbers"].ShouldBeOfExactType<List<int>>();
    [Fact] void should_keep_primitive_collection_elements() => ((List<int>)_clone["Numbers"]!).ShouldContainOnly(1, 2, 3);
    [Fact] void should_keep_string() => _clone["Text"].ShouldEqual("text");
    [Fact] void should_keep_guid() => _clone["Id"].ShouldEqual(_guid);
    [Fact] void should_keep_byte_array_content() => ((byte[])_clone["Bytes"]!).ShouldContainOnly(_bytes);
    [Fact] void should_keep_primitive() => _clone["Number"].ShouldEqual(42);
}
