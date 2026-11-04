// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations.for_EventMigrationBuilder.given;
using Cratis.Chronicle.Events.Migrations.for_EventMigrationPropertyBuilder.given;

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationPropertyBuilder.when_serializing_object_literals;

public class and_members_have_explicit_json_names : Specification
{
    EventMigrationPropertyBuilder _builder;

    void Establish() => _builder = new(new PrefixPolicy());
    void Because() => _builder.DefaultValue("mapped_Defaults", new AttributedDefaults(new("global"), "other"));

    [Fact] void should_keep_the_explicit_name() => _builder.Properties["mapped_Defaults"]["$defaultValue"]!["Explicit_Module"]!.ToJsonString().ShouldEqual("\"global\"");
    [Fact] void should_apply_the_policy_to_other_members() => _builder.Properties["mapped_Defaults"]["$defaultValue"]!["mapped_Other"]!.ToJsonString().ShouldEqual("\"other\"");
}
