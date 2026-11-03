// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_Projections.when_registering_explicitly;

public class a_model_with_only_projection_identity : given.projections_for_explicit_registration
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(_projections.Register<IdentityOnly>);

    [Fact] void should_still_require_mapping_annotations() => _error.ShouldBeOfExactType<ReadModelIsNotModelBound>();
    [Fact] void should_not_publish_a_definition() => _projections.Definitions.ShouldBeEmpty();
    [Fact] void should_not_publish_a_handler() => _projections.GetAllHandlers().ShouldBeEmpty();
    [Fact] void should_not_register_remotely() => _registrations.ShouldBeEmpty();

    [Projection("identity-only")]
    public record IdentityOnly;
}
