// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.EventTypes.for_MigrationExpressionSources;

public class when_extracting : Specification
{
    static string[] From(string json) => [.. MigrationExpressionSources.From(JsonNode.Parse(json))];

    [Fact] void should_extract_the_property_of_a_path() => From("\"@.name\"").ShouldEqual(["name"]);
    [Fact] void should_ignore_a_string_that_is_not_a_path() => From("\"literal\"").ShouldBeEmpty();
    [Fact] void should_ignore_a_number() => From("5").ShouldBeEmpty();
    [Fact] void should_extract_the_old_name_of_a_rename() => From("{\"$rename\":\"old\"}").ShouldEqual(["old"]);
    [Fact] void should_ignore_a_rename_that_is_not_a_string() => From("{\"$rename\":5}").ShouldBeEmpty();
    [Fact] void should_extract_the_source_of_a_split() => From("{\"$split\":{\"source\":\"full\"}}").ShouldEqual(["full"]);
    [Fact] void should_ignore_a_split_without_a_source() => From("{\"$split\":{}}").ShouldBeEmpty();
    [Fact] void should_extract_every_source_of_a_combine() => From("{\"$combine\":{\"sources\":[\"a\",\"b\"]}}").ShouldEqual(["a", "b"]);
    [Fact] void should_ignore_a_combine_without_sources() => From("{\"$combine\":{}}").ShouldBeEmpty();
    [Fact] void should_extract_the_source_of_map_values() => From("{\"$mapValues\":{\"source\":\"kind\"}}").ShouldEqual(["kind"]);
    [Fact] void should_ignore_an_unknown_expression() => From("{\"$other\":{\"source\":\"x\"}}").ShouldBeEmpty();
    [Fact] void should_ignore_an_object_with_more_than_one_entry() => From("{\"$rename\":\"a\",\"$split\":{\"source\":\"b\"}}").ShouldBeEmpty();
    [Fact] void should_ignore_null() => MigrationExpressionSources.From(null).ShouldBeEmpty();
}
