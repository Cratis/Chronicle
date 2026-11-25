// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventEntryConverter;

public class when_reading_an_empty_schema : given.a_schema_aware_read
{
    IDictionary<string, object?> _content;

    void Establish()
    {
        schema = JsonSchema.FromJson("""{"type":"object"}""");
        entry.Content = """{"2":{"Name":"value","number":42,"empty":null,"nested":{"empty":null},"items":[1,true,null]}}""";
    }

    async Task Because()
    {
        result = await Read();
        _content = result.Content;
    }

    [Fact] void should_keep_original_property_names() => _content["Name"].ShouldEqual("value");
    [Fact] void should_read_numbers_as_doubles() => _content["number"].ShouldEqual(42d);
    [Fact] void should_omit_top_level_nulls() => _content.ContainsKey("empty").ShouldBeFalse();
    [Fact] void should_keep_nested_nulls() => ((IDictionary<string, object?>)_content["nested"]!)["empty"].ShouldBeNull();
    [Fact] void should_keep_array_values() => ((object?[])_content["items"]!).ShouldEqual(new object?[] { 1d, true, null });
}
