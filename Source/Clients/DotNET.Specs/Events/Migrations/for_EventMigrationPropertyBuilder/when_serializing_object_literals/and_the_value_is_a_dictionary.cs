// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations.for_EventMigrationBuilder.given;
using Cratis.Chronicle.Events.Migrations.for_EventMigrationPropertyBuilder.given;

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationPropertyBuilder.when_serializing_object_literals;

public class and_the_value_is_a_dictionary : Specification
{
    EventMigrationPropertyBuilder _builder;

    void Establish() => _builder = new(new PrefixPolicy());
    void Because() => _builder.DefaultValue("mapped_Values", new WithDictionary(new() { ["Key"] = 1 }));

    [Fact] void should_apply_the_policy_to_the_member() => _builder.Properties["mapped_Values"]["$defaultValue"]!["mapped_Values"]!["Key"]!.ToJsonString().ShouldEqual("1");
}
