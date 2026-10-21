// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Projections.for_ImmediateProjection.when_getting_model_instance;

public class and_the_passive_model_has_nullable_concepts : given.a_passive_projection_with_nullable_concepts
{
    ProjectionResult _result;
    given.Thing _model;

    async Task Because()
    {
        _result = await _grain.GetModelInstance();
        _model = JsonSerializer.Deserialize<given.Thing>(_result.ReadModel!.ToJsonString(), _serializerOptions)!;
    }

    [Fact] void should_populate_the_nullable_endpoint() => _model.Endpoint.ShouldEqual(new given.Endpoint(ConfiguredEndpoint));
    [Fact] void should_populate_the_name() => _model.Name.ShouldEqual(ConfiguredName);
    [Fact] void should_keep_the_unset_nullable_bool_concept_null() => _model.Enabled.ShouldBeNull();
    [Fact] void should_populate_the_identifier() => _model.Id.ShouldEqual(new given.ThingId(ThingKey));
    [Fact] void should_project_the_configuring_event() => _result.ProjectedEventsCount.ShouldEqual(1);
}
