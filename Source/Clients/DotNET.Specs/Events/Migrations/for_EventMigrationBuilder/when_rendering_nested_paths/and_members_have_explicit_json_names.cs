// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationBuilder.when_rendering_nested_paths;

public class and_members_have_explicit_json_names : Specification
{
    EventMigrationBuilder _builder;
    JsonObject _result;

    void Establish() => _builder = new(new given.PrefixPolicy());

    void Because()
    {
        new EventMigrationBuilderFor<given.AttributedTarget, given.AttributedSource>(_builder).Properties(properties => properties
            .RenamedFrom(target => target.Contact.Email, source => source.Contact.EmailAddress)
            .Split(target => target.Contact.FirstName, source => source.Contact.FullName, " ", 0)
            .Combine(target => target.Contact.FullName, " ", source => source.Contact.FirstName, source => source.Contact.LastName)
            .MapValues(target => target.Contact.Status, source => source.Contact.Status, map => map.Map("OldValue", "NewValue"))
            .DefaultValue(target => target.Contact.Label, "DoNotRename"));
        _result = _builder.ToJson();
    }

    [Fact] void should_honor_attribute_names_for_rename() => _result["WireTarget.WireEmail"]!["$rename"]!.GetValue<string>().ShouldEqual("WireSource.WireAddress");
    [Fact] void should_apply_policy_only_to_unannotated_split_members() => _result["WireTarget.mapped_FirstName"]!["$split"]!["source"]!.GetValue<string>().ShouldEqual("WireSource.mapped_FullName");
    [Fact] void should_resolve_every_combined_source() => _result["WireTarget.mapped_FullName"]!["$combine"]!["sources"]!.AsArray().Select(_ => _!.GetValue<string>()).ShouldEqual(["WireSource.mapped_FirstName", "WireSource.mapped_LastName"]);
    [Fact] void should_resolve_the_value_map() => _result["WireTarget.NewStatus"]!["$mapValues"]!["source"]!.GetValue<string>().ShouldEqual("WireSource.OldStatus");
    [Fact] void should_resolve_the_default_target_without_renaming_the_literal() => _result["WireTarget.mapped_Label"]!["$defaultValue"]!.GetValue<string>().ShouldEqual("DoNotRename");
}
