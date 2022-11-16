// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Specifications.Integration;

namespace Aksio.Cratis.Events.Projections.Scenarios.when_projecting_properties;

public class with_simple_set_operations : ProjectionSpecificationFor<Model>
{
    string model_id;
    Event event_appended;
    ProjectionResult<Model> result;

    protected override IProjectionFor<Model> CreateProjection() => new SetPropertiesProjection();

    async Task Because()
    {
        model_id = Guid.NewGuid().ToString();
        event_appended = new Event(
            "Forty Two",
            true,
            42,
            42.42,
            "Forty Three",
            true,
            43,
            43.43);
        await context.EventLog.Append(model_id, event_appended);
        result = await context.GetById(model_id);
    }

    [Fact] void should_set_the_string_value() => result.Model.StringValue.ShouldEqual(event_appended.StringValue);
    [Fact] void should_set_the_bool_value() => result.Model.BoolValue.ShouldEqual(event_appended.BoolValue);
    [Fact] void should_set_the_int_value() => result.Model.IntValue.ShouldEqual(event_appended.IntValue);
    [Fact] void should_set_the_double_value() => result.Model.DoubleValue.ShouldEqual(event_appended.DoubleValue);
    [Fact] void should_set_the_string_concept_value() => result.Model.StringConceptValue.ShouldEqual(event_appended.StringConceptValue);
    [Fact] void should_set_the_bool_concept_value() => result.Model.BoolConceptValue.ShouldEqual(event_appended.BoolConceptValue);
    [Fact] void should_set_the_int_concept_value() => result.Model.IntConceptValue.ShouldEqual(event_appended.IntConceptValue);
    [Fact] void should_set_the_double_concept_value() => result.Model.DoubleConceptValue.ShouldEqual(event_appended.DoubleConceptValue);
}
