// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationBuilder;

public class and_the_default_naming_policy_is_used : Specification
{
    EventMigrationBuilder _builder;
    JsonObject _result;

    void Establish()
    {
        _builder = new EventMigrationBuilder();
        _builder.Properties(pb => pb.RenamedFrom("Email", "EmailAddress"));
    }

    void Because() => _result = _builder.ToJson();

    [Fact] void should_keep_the_declared_target_name() => _result["Email"].ShouldNotBeNull();

    [Fact] void should_keep_the_declared_source_name() => _result["Email"]!["$rename"]!.GetValue<string>().ShouldEqual("EmailAddress");
}
