// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Serialization;

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationBuilder;

public class and_a_camel_case_naming_policy_is_configured : Specification
{
    EventMigrationBuilder _builder;
    JsonObject _result;

    void Establish()
    {
        _builder = new EventMigrationBuilder(new CamelCaseNamingPolicy());
        new EventMigrationBuilderFor<given.TargetEvent, given.SourceEvent>(_builder).Properties(properties => properties
            .RenamedFrom(target => target.Email, source => source.EmailAddress)
            .Split(target => target.FirstName, source => source.FullName, " ", 0)
            .Combine(target => target.FullName, " ", source => source.FirstName, source => source.LastName)
            .MapValues(target => target.Status, source => source.Status, map => map.Map(0, 10)));
    }

    void Because() => _result = _builder.ToJson();

    [Fact] void should_name_the_renamed_target_the_way_the_payload_does() => _result["email"].ShouldNotBeNull();

    [Fact] void should_name_the_renamed_source_the_way_the_payload_does() => _result["email"]!["$rename"]!.GetValue<string>().ShouldEqual("emailAddress");

    [Fact] void should_name_the_split_source_the_way_the_payload_does() => _result["firstName"]!["$split"]!["source"]!.GetValue<string>().ShouldEqual("fullName");

    [Fact] void should_name_the_combined_sources_the_way_the_payload_does() => _result["fullName"]!["$combine"]!["sources"]!.AsArray().Select(_ => _!.GetValue<string>()).ShouldContainOnly(["firstName", "lastName"]);

    [Fact] void should_name_the_mapped_source_the_way_the_payload_does() => _result["status"]!["$mapValues"]!["source"]!.GetValue<string>().ShouldEqual("status");
}
