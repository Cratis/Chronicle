// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations.for_EventMigrationBuilder.given;
using Cratis.Chronicle.Events.Migrations.for_EventMigrationPropertyBuilder.given;
using Cratis.Serialization;

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationPropertyBuilder.when_serializing_object_literals;

public class and_builders_with_different_policies_are_interleaved : Specification
{
    EventMigrationPropertyBuilder _prefix;
    EventMigrationPropertyBuilder _camel;
    EventMigrationPropertyBuilder _default;

    void Establish()
    {
        _prefix = new(new PrefixPolicy());
        _camel = new(new CamelCaseNamingPolicy());
        _default = new();
    }

    void Because()
    {
        _prefix.DefaultValue("A", new ModuleDefaults(new("global")));
        _camel.DefaultValue("A", new ModuleDefaults(new("global")));
        _default.DefaultValue("A", new ModuleDefaults(new("global")));
        _prefix.DefaultValue("B", new ModuleDefaults(new("global")));
    }

    [Fact] void should_prefix_for_the_prefix_policy() => _prefix.Properties["A"]["$defaultValue"]!.ToJsonString().ShouldEqual("{\"mapped_Module\":\"global\"}");
    [Fact] void should_camel_case_for_the_camel_case_policy() => _camel.Properties["A"]["$defaultValue"]!.ToJsonString().ShouldEqual("{\"module\":\"global\"}");
    [Fact] void should_keep_names_for_the_default_policy() => _default.Properties["A"]["$defaultValue"]!.ToJsonString().ShouldEqual("{\"Module\":\"global\"}");
    [Fact] void should_still_prefix_after_other_policies_were_used() => _prefix.Properties["B"]["$defaultValue"]!.ToJsonString().ShouldEqual("{\"mapped_Module\":\"global\"}");
}
