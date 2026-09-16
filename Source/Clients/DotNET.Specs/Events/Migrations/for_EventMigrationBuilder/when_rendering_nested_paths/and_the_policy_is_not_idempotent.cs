// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationBuilder.when_rendering_nested_paths;

public class and_the_policy_is_not_idempotent : Specification
{
    EventMigrationBuilder _builder;
    JsonObject _result;

    void Establish() => _builder = new(new given.PrefixPolicy());

    void Because()
    {
        new EventMigrationBuilderFor<given.TargetEvent, given.SourceEvent>(_builder).Properties(properties =>
            properties.RenamedFrom(target => target.Contact.Email, source => source.Contact.EmailAddress));
        _result = _builder.ToJson();
    }

    [Fact] void should_render_each_target_segment_exactly_once() => _result.ContainsKey("mapped_Contact.mapped_Email").ShouldBeTrue();
    [Fact] void should_render_each_source_segment_exactly_once() => _result["mapped_Contact.mapped_Email"]!["$rename"]!.GetValue<string>().ShouldEqual("mapped_Contact.mapped_EmailAddress");
}
