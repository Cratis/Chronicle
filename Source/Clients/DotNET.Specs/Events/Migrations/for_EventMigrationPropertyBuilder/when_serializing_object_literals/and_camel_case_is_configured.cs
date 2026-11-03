// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations.for_EventMigrationPropertyBuilder.given;
using Cratis.Serialization;

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationPropertyBuilder.when_serializing_object_literals;

public class and_camel_case_is_configured : Specification
{
    EventMigrationPropertyBuilder _builder;

    void Establish() => _builder = new(new CamelCaseNamingPolicy());
    void Because() => _builder.DefaultValue("Defaults", new ModuleDefaults(new("global")));

    [Fact] void should_use_the_camel_cased_member_name() => _builder.Properties["Defaults"]["$defaultValue"]!["module"]!.ToJsonString().ShouldEqual("\"global\"");
    [Fact] void should_not_keep_the_pascal_cased_member_name() => _builder.Properties["Defaults"]["$defaultValue"]!.AsObject().ContainsKey("Module").ShouldBeFalse();
}
