// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaAnnotations;

public class when_normalizing_schema_declarations : Specification
{
    JsonNode _schema;
    JsonNode _normalized;
    IReadOnlyList<(string Keyword, string Path)> _found;

    void Establish() => _schema = JsonNode.Parse("""
        {"title":"Event","description":"Useful","deprecated":false,"examples":[],"$comment":"old",
         "properties":{"deprecated":{"type":"object","default":{"deprecated":true},"const":{"contentEncoding":"data"},"enum":[{"examples":1}]},
                       "bytes":{"type":"string","contentEncoding":"base64"}},
         "$defs":{"a/b~c":{"deprecated":true}}}
        """);

    void Because()
    {
        _found = JsonSchemaAnnotations.FindAnnotationKeywords(_schema);
        _normalized = JsonSchemaAnnotations.RemoveAnnotations(_schema);
    }

    [Fact] void should_find_only_declaration_annotations() => _found.Count.ShouldEqual(5);
    [Fact] void should_report_root_path() => _found.ShouldContain(("deprecated", "$/deprecated"));
    [Fact] void should_report_property_path() => _found.ShouldContain(("contentEncoding", "$/properties/bytes/contentEncoding"));
    [Fact] void should_escape_schema_paths() => _found.ShouldContain(("deprecated", "$/\u0024defs/a~1b~0c/deprecated"));
    [Fact] void should_keep_title() => _normalized["title"].GetValue<string>().ShouldEqual("Event");
    [Fact] void should_keep_description() => _normalized["description"].GetValue<string>().ShouldEqual("Useful");
    [Fact] void should_keep_payloads_and_property_names() => JsonNode.DeepEquals(_schema["properties"]["deprecated"], _normalized["properties"]["deprecated"]).ShouldBeTrue();
    [Fact] void should_leave_no_cleanup_annotations() => JsonSchemaAnnotations.FindAnnotationKeywords(_normalized).ShouldBeEmpty();
    [Fact] void should_be_idempotent() => JsonNode.DeepEquals(_normalized, JsonSchemaAnnotations.RemoveAnnotations(_normalized)).ShouldBeTrue();
    [Fact] void should_not_mutate_input() => _schema["deprecated"].ShouldNotBeNull();
    [Fact] void should_preserve_existing_title_and_description_tolerance() => JsonSchemaCompatibilityExtensions.EqualsIgnoringAnnotations("""{"title":"Old","description":"Old","properties":{"x":{"type":"string","title":"Old","description":"Old"}}}""", """{"title":"New","description":"New","properties":{"x":{"type":"string","title":"New","description":"New"}}}""").ShouldBeTrue();
}
