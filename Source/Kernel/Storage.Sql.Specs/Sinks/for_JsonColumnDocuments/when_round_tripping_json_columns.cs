// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_JsonColumnDocuments;

public class when_round_tripping_json_columns : Specification
{
    IDictionary<string, object?> _document;
    object? _empty;
    string _nullObject;
    string _nullArray;
    IDictionary<string, object?> _roundTripped;

    void Because()
    {
        _document = (IDictionary<string, object?>)JsonColumnDocuments.DeserializeJsonColumn("""{"text":"a","whole":5,"fraction":1.5,"flag":true,"nothing":null,"items":[1,2],"nested":{"inner":"b"}}""")!;
        _empty = JsonColumnDocuments.DeserializeJsonColumn(string.Empty);
        _nullObject = JsonColumnDocuments.SerializeJsonColumn(null, Column(false));
        _nullArray = JsonColumnDocuments.SerializeJsonColumn(null, Column(true));

        var source = new ExpandoObject();
        var dictionary = (IDictionary<string, object?>)source;
        dictionary["concept"] = new given.a_value("wrapped");
        dictionary["list"] = new List<object?> { new given.a_value("item"), 3 };
        dictionary["map"] = new Dictionary<object, object> { ["key"] = new given.a_value("value") };
        dictionary["text"] = "plain";
        _roundTripped = (IDictionary<string, object?>)JsonColumnDocuments.DeserializeJsonColumn(JsonColumnDocuments.SerializeJsonColumn(source, Column(false)))!;
    }

    [Fact] void should_read_a_string() => _document["text"].ShouldEqual("a");
    [Fact] void should_read_a_whole_number_as_long() => _document["whole"].ShouldEqual(5L);
    [Fact] void should_read_a_fraction_as_decimal() => _document["fraction"].ShouldEqual(1.5m);
    [Fact] void should_read_a_boolean() => _document["flag"].ShouldEqual(true);
    [Fact] void should_read_null() => _document["nothing"].ShouldBeNull();
    [Fact] void should_read_an_array_as_a_list() => ((IList<object?>)_document["items"]!).Count.ShouldEqual(2);
    [Fact] void should_read_an_object_as_an_expando() => _document["nested"].ShouldBeOfExactType<ExpandoObject>();
    [Fact] void should_read_nothing_from_an_empty_string() => _empty.ShouldBeNull();
    [Fact] void should_serialize_null_as_an_empty_object_for_a_non_array_column() => _nullObject.ShouldEqual("{}");
    [Fact] void should_serialize_null_as_an_empty_array_for_an_array_column() => _nullArray.ShouldEqual("[]");
    [Fact] void should_unwrap_a_concept_property() => _roundTripped["concept"].ShouldEqual("wrapped");
    [Fact] void should_unwrap_a_concept_in_a_list() => ((IList<object?>)_roundTripped["list"]!)[0].ShouldEqual("item");
    [Fact] void should_serialize_a_dictionary_as_an_object() => ((IDictionary<string, object?>)_roundTripped["map"]!)["key"].ShouldEqual("value");
    [Fact] void should_keep_a_string() => _roundTripped["text"].ShouldEqual("plain");

    static ProjectedColumn Column(bool isArray) => new("column", typeof(string), false, true, isArray, true);
}
