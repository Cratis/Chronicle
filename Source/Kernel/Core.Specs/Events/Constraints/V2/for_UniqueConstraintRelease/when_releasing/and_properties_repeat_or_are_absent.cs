// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintRelease.when_releasing;

public class and_properties_repeat_or_are_absent : Specification
{
    JsonObject _content;
    JsonSchema _schema;
    IReadOnlyList<UniqueConstraintComponent> _components;
    string _original;

    void Establish()
    {
        _content = JsonNode.Parse("""{"profile":{"name":"Ada"},"empty":"","nil":null,"zero":0,"items":[]}""")!.AsObject();
        _original = _content.ToJsonString();
        _schema = JsonSchema.FromJson("""
            {"type":"object","properties":{
                "profile":{"type":"object","properties":{"name":{"type":"string"}}},
                "empty":{"type":"string"},"nil":{"type":"integer","format":"int32"},
                "absent":{"type":"integer","format":"int32","default":42},
                "zero":{"type":"integer","format":"int32"},"items":{"type":"array","items":{"type":"string"}}}}
            """);
    }

    void Because() => _components = new UniqueConstraintComponentsBuilder(new ExpandoObjectConverter(new TypeFormats())).Build(
        new UniqueConstraintEventDefinition("event", ["PROFILE.Name", "empty", "profile.name", "nil", "absent", "zero", "items", "unknown"]), _content, _schema);

    [Fact] void should_keep_every_declared_position() => _components.Count.ShouldEqual(8);
    [Fact] void should_resolve_nested_names_case_insensitively() => _components[0].Value.ShouldEqual("Ada");
    [Fact] void should_keep_repeated_paths() => _components[2].ShouldEqual(_components[0]);
    [Fact] void should_keep_empty_strings_present() => _components[1].Value.ShouldEqual("");
    [Fact] void should_keep_literal_null() => _components[3].Value.ShouldBeNull();
    [Fact] void should_not_fill_a_missing_number_with_a_default() => _components[4].Value.ShouldBeNull();
    [Fact] void should_keep_numeric_zero_present() => _components[5].Value.ShouldEqual(0);
    [Fact] void should_keep_empty_arrays_present() => ((object?[])_components[6].Value!).Length.ShouldEqual(0);
    [Fact] void should_mark_missing_declared_properties_as_in_schema() => _components[4].IsInSchema.ShouldBeTrue();
    [Fact] void should_mark_only_undeclared_properties_as_outside_schema() => _components[7].IsInSchema.ShouldBeFalse();
    [Fact] void should_not_mutate_the_source() => _content.ToJsonString().ShouldEqual(_original);
}
