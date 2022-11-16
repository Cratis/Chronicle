// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Specifications.Integration;

namespace Aksio.Cratis.Events.Projections.Scenarios.when_projecting_properties;

public class with_set_to_value_operations : ProjectionSpecificationFor<Model>
{
    string model_id;
    ProjectionResult<Model> result;
    protected override IProjectionFor<Model> CreateProjection() => new SetValuesProjection();

    async Task Because()
    {
        model_id = Guid.NewGuid().ToString();
        await context.EventLog.Append(model_id, new EmptyEvent());
        result = await context.GetById(model_id);
    }

    [Fact] void should_set_the_string_value() => result.Model.StringValue.ShouldEqual(SetValuesProjection.StringValue);
    [Fact] void should_set_the_bool_value() => result.Model.BoolValue.ShouldEqual(SetValuesProjection.BoolValue);
    [Fact] void should_set_the_int_value() => result.Model.IntValue.ShouldEqual(SetValuesProjection.IntValue);
    [Fact] void should_set_the_double_value() => result.Model.DoubleValue.ShouldEqual(SetValuesProjection.DoubleValue);
    [Fact] void should_set_the_string_concept_value() => result.Model.StringConceptValue.ShouldEqual(SetValuesProjection.StringConceptValue);
    [Fact] void should_set_the_bool_concept_value() => result.Model.BoolConceptValue.ShouldEqual(SetValuesProjection.BoolConceptValue);
    [Fact] void should_set_the_int_concept_value() => result.Model.IntConceptValue.ShouldEqual(SetValuesProjection.IntConceptValue);
    [Fact] void should_set_the_double_concept_value() => result.Model.DoubleConceptValue.ShouldEqual(SetValuesProjection.DoubleConceptValue);}
