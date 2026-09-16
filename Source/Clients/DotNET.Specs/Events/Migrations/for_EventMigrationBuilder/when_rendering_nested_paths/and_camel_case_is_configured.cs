// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Serialization;

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationBuilder.when_rendering_nested_paths;

public class and_camel_case_is_configured : Specification
{
    EventMigrationBuilder _builder;
    JsonObject _result;

    void Establish() => _builder = new(new CamelCaseNamingPolicy());

    void Because()
    {
        _builder.Properties(properties =>
        {
            properties.RenamedFrom("Contact.Email", "Contact.EmailAddress");
            properties.Split("Contact.FirstName", "Contact.FullName", " ", 0);
            properties.Combine("Contact.FullName", " ", "Contact.FirstName", "Contact.LastName");
            properties.MapValues("Contact.Status", "Contact.OldStatus", [new ValueMapping("OldValue", "NewValue")]);
            properties.DefaultValue("Contact.Label", "DoNotRename");
        });
        _result = _builder.ToJson();
    }

    [Fact] void should_render_each_rename_segment() => _result["contact.email"]!["$rename"]!.GetValue<string>().ShouldEqual("contact.emailAddress");
    [Fact] void should_render_each_split_segment() => _result["contact.firstName"]!["$split"]!["source"]!.GetValue<string>().ShouldEqual("contact.fullName");
    [Fact] void should_render_each_combined_segment() => _result["contact.fullName"]!["$combine"]!["sources"]!.AsArray().Select(_ => _!.GetValue<string>()).ShouldEqual(["contact.firstName", "contact.lastName"]);
    [Fact] void should_render_each_mapped_segment() => _result["contact.status"]!["$mapValues"]!["source"]!.GetValue<string>().ShouldEqual("contact.oldStatus");
    [Fact] void should_preserve_literal_default_values() => _result["contact.label"]!["$defaultValue"]!.GetValue<string>().ShouldEqual("DoNotRename");
    [Fact] void should_preserve_literal_mapped_values() => _result["contact.status"]!["$mapValues"]!["mappings"]![0]!["to"]!.GetValue<string>().ShouldEqual("NewValue");
}
