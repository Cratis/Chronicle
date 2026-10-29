// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Registrations.for_ExplicitArtifacts;

public class when_registering_a_model_with_projection_identity : Specification
{
    ExplicitArtifacts _modelBound;
    ExplicitArtifacts _callback;
    ExplicitArtifacts _override;
    ExplicitArtifacts _replacement;

    void Because()
    {
        _modelBound = new ExplicitArtifacts().RegisterReadModel<Model>();
        _callback = new ExplicitArtifacts().RegisterProjection<Model>(_ => { });
        _override = new ExplicitArtifacts().RegisterProjection<Model>(_ => { }, "callback-id");
        _replacement = new ExplicitArtifacts().RegisterReadModel<Model>().RegisterReadModel<Model>();
    }

    [Fact] void should_use_the_attribute_for_model_bound_options() => _modelBound.Projections.Single().Id.ShouldEqual((ProjectionId)"stable-model");
    [Fact] void should_use_the_attribute_for_callback_defaults() => _callback.Projections.Single().Id.ShouldEqual((ProjectionId)"stable-model");
    [Fact] void should_prefer_an_explicit_callback_id() => _override.Projections.Single().Id.ShouldEqual((ProjectionId)"callback-id");
    [Fact] void should_keep_repeated_options_registration_idempotent() => _replacement.Projections.Count().ShouldEqual(1);

    [Projection("stable-model")]
    public record Model;
}
