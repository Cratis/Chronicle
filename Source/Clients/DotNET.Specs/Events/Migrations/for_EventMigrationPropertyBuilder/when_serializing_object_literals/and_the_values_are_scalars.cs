// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations.for_EventMigrationBuilder.given;
using Cratis.Chronicle.Events.Migrations.for_EventTypeMigration.given;

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationPropertyBuilder.when_serializing_object_literals;

public class and_the_values_are_scalars : Specification
{
    EventMigrationPropertyBuilder _prefix;
    EventMigrationPropertyBuilder _default;

    void Establish()
    {
        _prefix = new(new PrefixPolicy());
        _default = new();
    }

    void Because()
    {
        foreach (var builder in new[] { _prefix, _default })
        {
            builder.DefaultValue("String", "text");
            builder.DefaultValue("Int", 42);
            builder.DefaultValue("Concept", new ModuleKey("global"));
            builder.DefaultValue("Enum", Level.High);
        }
    }

    [Fact] void should_render_strings_as_with_the_default_policy() => _prefix.Properties["String"].ToJsonString().ShouldEqual(_default.Properties["String"].ToJsonString());
    [Fact] void should_render_integers_as_with_the_default_policy() => _prefix.Properties["Int"].ToJsonString().ShouldEqual(_default.Properties["Int"].ToJsonString());
    [Fact] void should_render_concepts_as_with_the_default_policy() => _prefix.Properties["Concept"].ToJsonString().ShouldEqual(_default.Properties["Concept"].ToJsonString());
    [Fact] void should_render_enums_as_with_the_default_policy() => _prefix.Properties["Enum"].ToJsonString().ShouldEqual(_default.Properties["Enum"].ToJsonString());
    [Fact] void should_render_concepts_as_their_value() => _prefix.Properties["Concept"]["$defaultValue"]!.ToJsonString().ShouldEqual("\"global\"");

    enum Level
    {
        Low = 0,
        High = 7
    }
}
