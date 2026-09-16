// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationBuilder.when_rendering_nested_paths;

public class and_an_override_declares_its_own_json_name : Specification
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

    [Fact] void should_use_the_overrides_target_name() => _result.ContainsKey("DerivedWireName").ShouldBeTrue();
    [Fact] void should_use_the_overrides_source_name() => _result["DerivedWireName"]!["$rename"]!.GetValue<string>().ShouldEqual("DerivedWireName");

    class Base
    {
        [JsonPropertyName("BaseWireName")]
        public virtual string Name { get; set; } = "base";
    }

    class Derived : Base
    {
        [JsonPropertyName("DerivedWireName")]
        public override string Name { get; set; } = "derived";
    }
}
