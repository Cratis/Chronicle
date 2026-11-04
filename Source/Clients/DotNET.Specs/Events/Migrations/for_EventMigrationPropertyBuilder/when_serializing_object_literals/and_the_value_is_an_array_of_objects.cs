// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations.for_EventMigrationBuilder.given;
using Cratis.Chronicle.Events.Migrations.for_EventMigrationPropertyBuilder.given;
using Cratis.Chronicle.Events.Migrations.for_EventTypeMigration.given;

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationPropertyBuilder.when_serializing_object_literals;

public class and_the_value_is_an_array_of_objects : Specification
{
    EventMigrationPropertyBuilder _builder;

    void Establish() => _builder = new(new PrefixPolicy());

    void Because()
    {
        _builder.DefaultValue("mapped_Items", new[] { new ModuleDefaults(new("a")), new ModuleDefaults(new("b")) });
        _builder.DefaultValue("mapped_Keys", new[] { new ModuleKey("a"), new ModuleKey("b") });
    }

    [Fact] void should_prefix_members_of_the_first_item() => _builder.Properties["mapped_Items"]["$defaultValue"]![0]!["mapped_Module"]!.ToJsonString().ShouldEqual("\"a\"");
    [Fact] void should_prefix_members_of_the_second_item() => _builder.Properties["mapped_Items"]["$defaultValue"]![1]!["mapped_Module"]!.ToJsonString().ShouldEqual("\"b\"");
    [Fact] void should_still_unwrap_concept_arrays() => _builder.Properties["mapped_Keys"]["$defaultValue"]!.ToJsonString().ShouldEqual("[\"a\",\"b\"]");
}
