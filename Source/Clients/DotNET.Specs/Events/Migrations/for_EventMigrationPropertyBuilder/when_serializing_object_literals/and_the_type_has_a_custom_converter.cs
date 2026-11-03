// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations.for_EventMigrationBuilder.given;
using Cratis.Chronicle.Events.Migrations.for_EventMigrationPropertyBuilder.given;

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationPropertyBuilder.when_serializing_object_literals;

public class and_the_type_has_a_custom_converter : Specification
{
    EventMigrationPropertyBuilder _builder;

    void Establish() => _builder = new(new PrefixPolicy());
    void Because() => _builder.DefaultValue("mapped_Custom", new CustomWritten("x"));

    [Fact] void should_leave_the_converter_output_unchanged() => _builder.Properties["mapped_Custom"]["$defaultValue"]!.ToJsonString().ShouldEqual("{\"Fixed_Name\":\"x\"}");
}
