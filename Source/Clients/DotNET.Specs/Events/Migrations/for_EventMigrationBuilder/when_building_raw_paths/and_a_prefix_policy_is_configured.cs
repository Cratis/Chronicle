// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationBuilder.when_building_raw_paths;

public class and_a_prefix_policy_is_configured : Specification
{
    EventMigrationBuilder _builder;
    JsonObject _result;

    void Establish() => _builder = new(new given.PrefixPolicy());

    void Because()
    {
        _builder.Properties(properties =>
        {
            properties.RenamedFrom("mapped_Contact.mapped_Email", "mapped_Contact.mapped_EmailAddress");
            properties.Split("mapped_Contact.mapped_FirstName", "mapped_Contact.mapped_FullName", "Keep Separator", 0);
            properties.Combine("mapped_Contact.mapped_FullName", "Keep Separator", "mapped_Contact.mapped_FirstName", "mapped_Contact.mapped_LastName");
            properties.DefaultValue("mapped_Contact.mapped_Label", "Keep Value");
            properties.MapValues("mapped_Contact.mapped_Status", "mapped_Contact.mapped_OldStatus", [new ValueMapping("Old", "New")]);
        });
        _result = _builder.ToJson();
    }

    [Fact] void should_preserve_the_rename_paths() => _result["mapped_Contact.mapped_Email"]!["$rename"]!.GetValue<string>().ShouldEqual("mapped_Contact.mapped_EmailAddress");
    [Fact] void should_preserve_the_split_paths() => _result["mapped_Contact.mapped_FirstName"]!["$split"]!["source"]!.GetValue<string>().ShouldEqual("mapped_Contact.mapped_FullName");
    [Fact] void should_preserve_the_combined_paths() => _result["mapped_Contact.mapped_FullName"]!["$combine"]!["sources"]!.AsArray().Select(_ => _!.GetValue<string>()).ShouldEqual(["mapped_Contact.mapped_FirstName", "mapped_Contact.mapped_LastName"]);
    [Fact] void should_preserve_the_default_path_and_value() => _result["mapped_Contact.mapped_Label"]!["$defaultValue"]!.GetValue<string>().ShouldEqual("Keep Value");
    [Fact] void should_preserve_the_value_map_paths() => _result["mapped_Contact.mapped_Status"]!["$mapValues"]!["source"]!.GetValue<string>().ShouldEqual("mapped_Contact.mapped_OldStatus");
    [Fact] void should_preserve_the_separator() => _result["mapped_Contact.mapped_FirstName"]!["$split"]!["separator"]!.GetValue<string>().ShouldEqual("Keep Separator");
    [Fact] void should_preserve_the_mapped_values() => _result["mapped_Contact.mapped_Status"]!["$mapValues"]!["mappings"]![0]!["to"]!.GetValue<string>().ShouldEqual("New");
}
