// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations.for_EventMigrationBuilder.given;
using Cratis.Chronicle.Events.Migrations.for_EventMigrationPropertyBuilder.given;

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationPropertyBuilder.when_serializing_object_literals;

public class and_a_prefix_policy_is_configured : Specification
{
    EventMigrationPropertyBuilder _builder;

    void Establish() => _builder = new(new PrefixPolicy());
    void Because() => _builder.DefaultValue("mapped_Settings", new ModuleSettings(new("global"), "Name", new ModuleDefaults(new("inner"))));

    [Fact] void should_prefix_the_member_name() => _builder.Properties["mapped_Settings"]["$defaultValue"]!["mapped_Module"]!.ToJsonString().ShouldEqual("\"global\"");
    [Fact] void should_prefix_the_nested_member_names() => _builder.Properties["mapped_Settings"]["$defaultValue"]!["mapped_Inner"]!["mapped_Module"]!.ToJsonString().ShouldEqual("\"inner\"");
    [Fact] void should_not_keep_the_original_member_name() => _builder.Properties["mapped_Settings"]["$defaultValue"]!.AsObject().ContainsKey("Module").ShouldBeFalse();
}
