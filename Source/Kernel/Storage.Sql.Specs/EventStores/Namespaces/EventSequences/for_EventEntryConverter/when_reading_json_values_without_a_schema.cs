// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventEntryConverter;

public class when_reading_json_values_without_a_schema : Specification
{
    IDictionary<string, object?> _result;

    void Because() => _result = EventEntryConverter.GetContentForGeneration(
        new EventEntry { Content = """{"1":{"text":"hello","yes":true,"no":false,"nothing":null,"fraction":1.5,"nested":{"inner":"x"},"items":[1,"a",{"k":true}]}}""" }, 1);

    [Fact] void should_read_strings() => _result["text"].ShouldEqual("hello");
    [Fact] void should_read_true() => _result["yes"].ShouldEqual(true);
    [Fact] void should_read_false() => _result["no"].ShouldEqual(false);
    [Fact] void should_read_null() => _result["nothing"].ShouldBeNull();
    [Fact] void should_read_fractions_as_decimal() => _result["fraction"].ShouldEqual(1.5m);
    [Fact] void should_read_nested_objects_as_expando() => ((IDictionary<string, object?>)(ExpandoObject)_result["nested"]!)["inner"].ShouldEqual("x");
    [Fact] void should_read_array_as_object_array() => _result["items"].ShouldBeOfExactType<object[]>();
    [Fact] void should_read_array_elements_in_order() => ((object[])_result["items"]!).Take(2).ShouldEqual([1L, "a"]);
    [Fact] void should_read_objects_within_arrays() => ((IDictionary<string, object?>)(ExpandoObject)((object[])_result["items"]!)[2])["k"].ShouldEqual(true);
}
