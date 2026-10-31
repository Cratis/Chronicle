// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_mapping_a_member_with_a_derived_function_name.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_mapping_a_member_with_a_derived_function_name;

public class and_both_source_and_target_are_named_week : Specification
{
    ReadModelScenario<WeekSummary> _scenario;

    void Establish() => _scenario = new ReadModelScenario<WeekSummary>().WithProjection(builder => builder
        .NoAutoMap()
        .From<WeekScheduled>(from => from.Set(model => model.Week).To(@event => @event.Week)));

    Task Because() => _scenario.Given.ForEventSource(EventSourceId.New()).Events(new WeekScheduled(23));
    void Destroy() => _scenario.Dispose();

    [Fact] void should_map_the_week() => _scenario.Instance!.Week.ShouldEqual(23);
}
