// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Events.Migrations.for_EventMigrationPropertyBuilder.given;
using Cratis.Chronicle.Events.Migrations.for_EventTypeMigration.given;

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationBuilder.when_rendering_object_literals;

public class and_a_prefix_policy_is_configured : Specification
{
    EventMigrationBuilder _builder;
    JsonObject _result;

    void Establish() => _builder = new(new for_EventMigrationBuilder.given.PrefixPolicy());

    void Because()
    {
        new EventMigrationBuilderFor<given.ObjectTarget, given.ObjectSource>(_builder).Properties(properties => properties
            .DefaultValue(target => target.Defaults, new ModuleDefaults(new ModuleKey("global"))));
        _result = _builder.ToJson();
    }

    [Fact] void should_prefix_the_property_path() => _result.ContainsKey("mapped_Defaults").ShouldBeTrue();
    [Fact] void should_prefix_the_literal_members() => _result["mapped_Defaults"]!["$defaultValue"]!["mapped_Module"]!.ToJsonString().ShouldEqual("\"global\"");
}
