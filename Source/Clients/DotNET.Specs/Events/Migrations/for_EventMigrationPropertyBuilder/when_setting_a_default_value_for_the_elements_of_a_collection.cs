// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;

namespace Cratis.Chronicle.Events.Migrations.for_EventMigrationPropertyBuilder;

public class when_setting_a_default_value_for_the_elements_of_a_collection : Specification
{
    record Day(string Activity);
    record Timesheet(IEnumerable<Day> Days);

    EventMigrationPropertyBuilder _builder;

    void Establish() => _builder = new EventMigrationPropertyBuilder(new CamelCaseNamingPolicy());

    void Because() => new EventMigrationPropertyBuilderFor<Timesheet, Timesheet>(_builder).DefaultValue(_ => _.Days.Select(day => day.Activity), "work");

    [Fact] void should_address_every_element_of_the_collection() => _builder.Properties.ContainsKey("days[].activity").ShouldBeTrue();
}
