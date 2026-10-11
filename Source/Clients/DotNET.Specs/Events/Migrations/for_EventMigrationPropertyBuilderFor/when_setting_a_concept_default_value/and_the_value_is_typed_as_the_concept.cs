// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations.for_EventTypeMigration.given;

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationPropertyBuilderFor.when_setting_a_concept_default_value;

public class and_the_value_is_typed_as_the_concept : Specification
{
    EventMigrationPropertyBuilder _inner;
    EventMigrationPropertyBuilderFor<ModuleAssigned, ModuleAssignedV1> _builder;

    void Establish()
    {
        _inner = new();
        _builder = new(_inner);
    }

    void Because() => _builder.DefaultValue<ModuleKey>(topic => topic.Module, new ModuleKey("global"));

    [Fact] void should_emit_the_underlying_primitive() => _inner.Properties["Module"]["$defaultValue"]!.ToJsonString().ShouldEqual("\"global\"");
}
