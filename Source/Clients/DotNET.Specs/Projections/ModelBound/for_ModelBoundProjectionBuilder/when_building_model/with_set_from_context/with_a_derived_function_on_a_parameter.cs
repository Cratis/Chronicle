// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_set_from_context;

public class with_a_derived_function_on_a_parameter : given.a_model_bound_projection_builder
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

    [Fact] void should_render_the_canonical_context_expression() => _result.From.Single().Value.Properties["Week"].ShouldEqual("$eventContext(Occurred.Week())");
    [Fact] void should_name_members_but_not_the_function() => _camelCased.From.Single().Value.Properties["week"].ShouldEqual("$eventContext(occurred.Week())");

    record Audit([Key] Guid Id, [SetFromContext<DebitAccountOpened>("Occurred.Week")] int Week);
}
