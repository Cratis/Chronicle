// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Serialization;

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationBuilder.when_building_raw_paths;

public class and_camel_case_is_configured : Specification
{
    EventMigrationBuilder _builder;
    JsonObject _result;

    void Establish() => _builder = new(new CamelCaseNamingPolicy());

    void Because()
    {
        _builder.Properties(properties =>
        {
            properties.RenamedFrom("WireContact.WireEmail", "WireContact.WireEmailAddress");
            properties.Split("WireContact.WireFirstName", "WireContact.WireFullName", "Keep Separator", 0);
            properties.Combine("WireContact.WireFullName", "Keep Separator", "WireContact.WireFirstName", "WireContact.WireLastName");
            properties.DefaultValue("WireContact.WireLabel", "Keep Value");
            properties.MapValues("WireContact.WireStatus", "WireContact.WireOldStatus", [new ValueMapping("Old", "New")]);
        });
        _result = _builder.ToJson();
    }

    [Fact] void should_preserve_the_rename_paths() => _result["WireContact.WireEmail"]!["$rename"]!.GetValue<string>().ShouldEqual("WireContact.WireEmailAddress");
    [Fact] void should_preserve_the_split_paths() => _result["WireContact.WireFirstName"]!["$split"]!["source"]!.GetValue<string>().ShouldEqual("WireContact.WireFullName");
    [Fact] void should_preserve_the_combined_paths() => _result["WireContact.WireFullName"]!["$combine"]!["sources"]!.AsArray().Select(_ => _!.GetValue<string>()).ShouldEqual(["WireContact.WireFirstName", "WireContact.WireLastName"]);
    [Fact] void should_preserve_the_default_path_and_value() => _result["WireContact.WireLabel"]!["$defaultValue"]!.GetValue<string>().ShouldEqual("Keep Value");
    [Fact] void should_preserve_the_value_map_paths() => _result["WireContact.WireStatus"]!["$mapValues"]!["source"]!.GetValue<string>().ShouldEqual("WireContact.WireOldStatus");
    [Fact] void should_preserve_the_separator() => _result["WireContact.WireFirstName"]!["$split"]!["separator"]!.GetValue<string>().ShouldEqual("Keep Separator");
    [Fact] void should_preserve_the_mapped_values() => _result["WireContact.WireStatus"]!["$mapValues"]!["mappings"]![0]!["to"]!.GetValue<string>().ShouldEqual("New");
}
