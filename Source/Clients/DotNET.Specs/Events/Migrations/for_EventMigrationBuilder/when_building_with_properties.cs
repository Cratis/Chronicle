// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationBuilder;

public class when_building_with_properties : Specification
{
    EventMigrationBuilder _builder;
    JsonObject _result;

    void Establish()
    {
        _builder = new EventMigrationBuilder();
        _builder.Properties(pb =>
        {
            pb.Split("FullName", " ", 0);
            pb.Combine("FirstName", "LastName");
        });
    }

    void Because() => _result = _builder.ToJson();

    [Fact] void should_have_two_properties() => _result.Count.ShouldEqual(2);

    [Fact] void should_contain_expressions() => _result.ToJsonString().ShouldContain("$");
}
