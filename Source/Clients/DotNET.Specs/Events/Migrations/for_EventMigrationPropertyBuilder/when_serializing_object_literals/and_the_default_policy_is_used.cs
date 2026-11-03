// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations.for_EventMigrationPropertyBuilder.given;
using Cratis.Serialization;

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationPropertyBuilder.when_serializing_object_literals;

public class and_the_default_policy_is_used : Specification
{
    EventMigrationPropertyBuilder _parameterless;
    EventMigrationPropertyBuilder _default;

    void Establish()
    {
        _parameterless = new();
        _default = new(new DefaultNamingPolicy());
    }

    void Because()
    {
        _parameterless.DefaultValue("Defaults", new ModuleDefaults(new("global")));
        _default.DefaultValue("Defaults", new ModuleDefaults(new("global")));
    }

    [Fact] void should_keep_the_pascal_cased_name_for_the_parameterless_builder() => _parameterless.Properties["Defaults"]["$defaultValue"]!.ToJsonString().ShouldEqual("{\"Module\":\"global\"}");
    [Fact] void should_keep_the_pascal_cased_name_for_the_default_policy() => _default.Properties["Defaults"]["$defaultValue"]!.ToJsonString().ShouldEqual("{\"Module\":\"global\"}");
}
