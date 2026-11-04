// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Migrations.for_EventTypeMigrations.when_carrying_content_between_generations;

public class and_a_downcast_removes_then_defaults_a_property : given.a_schema_filtered_chain
{
    void Establish() => _type = _type with { Generation = 3 };
    async Task Because() => _result = await _migrations.MigrateToAllGenerations(_definition, _type, _source, new(), Converted, Migrating);

    [Fact] void should_omit_the_removed_property_from_the_intermediate_generation() => Members(_result[2]).ContainsKey("status").ShouldBeFalse();
    [Fact] void should_default_the_reintroduced_property() => Members(_result[1])["status"].ShouldEqual("pending");
    [Fact] void should_keep_the_source_generation() => Members(_result[3])["status"].ShouldEqual("legacy");
    [Fact] void should_filter_the_input_of_the_second_edge() => _inputs[1].ContainsKey("status").ShouldBeFalse();
    [Fact] void should_keep_raw_conversion_callbacks() => _rawConversions[1]["status"]!.GetValue<string>().ShouldEqual("legacy");
}
