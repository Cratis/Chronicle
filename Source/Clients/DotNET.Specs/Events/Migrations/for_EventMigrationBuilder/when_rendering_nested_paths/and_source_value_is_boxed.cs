// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationBuilder.when_rendering_nested_paths;

public class and_source_value_is_boxed : Specification
{
    EventMigrationBuilder _builder;
    JsonObject _result;

    void Establish() => _builder = new(new given.PrefixPolicy());

    void Because()
    {
        new EventMigrationBuilderFor<Counted, Counted>(_builder).Properties(properties =>
            properties.RenamedFrom(target => target.Count.Total, source => source.Count.Total));
        _result = _builder.ToJson();
    }

    [Fact] void should_honor_the_nested_attribute_after_unboxing() => _result["mapped_Count.WireTotal"]!["$rename"]!.GetValue<string>().ShouldEqual("mapped_Count.WireTotal");

    record Counted(CountDetails Count);
    record CountDetails([property: JsonPropertyName("WireTotal")] int Total);
}
