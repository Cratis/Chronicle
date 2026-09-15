// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationPropertyBuilder;

/// <summary>
/// An event's payload carries an enum as its underlying number, so a map written in terms of enum members has to be
/// rendered the same way - a map of names would never match anything.
/// </summary>
public class when_mapping_enum_values : Specification
{
    EventMigrationPropertyBuilder _builder;

    void Establish() => _builder = new EventMigrationPropertyBuilder();

    void Because() => _builder.MapValues("Status", "Status", [new ValueMapping(PreviousStatus.Verified, CurrentStatus.Confirmed)]);

    [Fact] void should_render_the_source_member_as_its_underlying_value() =>
        _builder.Properties["Status"]["$mapValues"]["mappings"][0]["from"].GetValue<int>().ShouldEqual(1);

    [Fact] void should_render_the_target_member_as_its_underlying_value() =>
        _builder.Properties["Status"]["$mapValues"]["mappings"][0]["to"].GetValue<int>().ShouldEqual(11);

    enum PreviousStatus
    {
        Unknown = 0,
        Verified = 1
    }

    enum CurrentStatus
    {
        Unspecified = 10,
        Confirmed = 11
    }
}
