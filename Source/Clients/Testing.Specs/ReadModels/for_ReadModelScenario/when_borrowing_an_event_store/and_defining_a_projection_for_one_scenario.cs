// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_borrowing_an_event_store.given;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_mapping_a_member_with_a_derived_function_name.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_borrowing_an_event_store;

public class and_defining_a_projection_for_one_scenario : a_borrowed_store
{
    ReadModelScenario<WeekSummary> _defined;
    ReadModelScenario<WeekSummary> _undefined;
    Exception _undefinedError;

    void Establish()
    {
        _defined = Borrowed<WeekSummary>().WithProjection(builder => builder
            .NoAutoMap()
            .From<WeekScheduled>(from => from.Set(model => model.Week).To(@event => @event.Week)));
        _undefined = Borrowed<WeekSummary>();
    }

    async Task Because()
    {
        await _defined.Given.ForEventSource(EventSourceId.New()).Events(new WeekScheduled(23));
        await _undefined.Given.ForEventSource(EventSourceId.New()).Events(new WeekScheduled(24));
        _undefinedError = Catch.Exception(() => _ = _undefined.Instance);
    }

    [Fact] void should_fold_through_the_scenarios_projection() => _defined.Instance!.Week.ShouldEqual(23);
    [Fact] void should_not_give_the_projection_to_another_scenario() => _undefinedError.ShouldBeOfExactType<NoReadModelHandlerFound>();
}
