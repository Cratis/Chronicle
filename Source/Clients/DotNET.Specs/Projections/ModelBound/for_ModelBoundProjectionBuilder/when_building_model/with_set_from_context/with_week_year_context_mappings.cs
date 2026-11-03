// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_set_from_context;

public class with_week_year_context_mappings : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;
    ProjectionDefinition _camelCased;
    ModelBoundProjectionBuilder _camelCasingBuilder;

    void Establish() => _camelCasingBuilder = new ModelBoundProjectionBuilder(new given.CamelCasingNamingPolicy(), event_types);

    void Because()
    {
        _result = builder.Build(typeof(Audit));
        _camelCased = _camelCasingBuilder.Build(typeof(Audit));
    }

    [Fact] void should_render_the_parameter_mapping() => _result.From.Single().Value.Properties["OpenedYear"].ShouldEqual("$eventContext(Occurred.WeekYear())");
    [Fact] void should_render_the_property_mapping() => _result.From.Single().Value.Properties["Year"].ShouldEqual("$eventContext(Occurred.WeekYear())");
    [Fact] void should_render_the_from_every_mapping() => _result.All.Properties["LatestYear"].ShouldEqual("$eventContext(Occurred.WeekYear())");
    [Fact] void should_name_parameter_members_but_not_the_function() => _camelCased.From.Single().Value.Properties["openedYear"].ShouldEqual("$eventContext(occurred.WeekYear())");
    [Fact] void should_name_property_members_but_not_the_function() => _camelCased.From.Single().Value.Properties["year"].ShouldEqual("$eventContext(occurred.WeekYear())");
    [Fact] void should_name_from_every_members_but_not_the_function() => _camelCased.All.Properties["latestYear"].ShouldEqual("$eventContext(occurred.WeekYear())");

    record Audit(
        [Key] Guid Id,
        [SetFromContext<DebitAccountOpened>("Occurred.WeekYear")] int OpenedYear,
        [FromEvery(contextProperty: "Occurred.WeekYear()")] int LatestYear)
    {
        [SetFromContext<DebitAccountOpened>("Occurred.WeekYear()")]
        public int Year { get; init; }
    }
}
