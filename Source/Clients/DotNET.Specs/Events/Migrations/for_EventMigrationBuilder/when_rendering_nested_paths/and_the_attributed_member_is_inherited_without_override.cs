// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationBuilder.when_rendering_nested_paths;

public class and_the_attributed_member_is_inherited_without_override : Specification
{
    EventMigrationBuilder _builder;
    JsonObject _result;

    void Establish() => _builder = new(new given.PrefixPolicy());

    void Because()
    {
        new EventMigrationBuilderFor<Derived, Derived>(_builder).Properties(properties =>
            properties.RenamedFrom(target => target.Name, source => source.Name));
        _result = _builder.ToJson();
    }

    [Fact] void should_keep_the_inherited_target_name() => _result.ContainsKey("BaseWireName").ShouldBeTrue();
    [Fact] void should_keep_the_inherited_source_name() => _result["BaseWireName"]!["$rename"]!.GetValue<string>().ShouldEqual("BaseWireName");

    class Base
    {
        [JsonPropertyName("BaseWireName")]
        public virtual string Name { get; set; } = "base";
    }

    class Derived : Base;
}
