// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.for_EventStoreForTesting.when_sharing_application_metadata;

public class and_projecting_model_bound_read_models : given.application_metadata_sharing
{
    void Because()
    {
        _other = new(null, _boundArtifacts);
        _third = new(null, _boundArtifacts);
    }

    [Fact] void should_share_the_metadata() => _other.Metadata.ShouldBeSame(_third.Metadata);
    [Fact] void should_have_the_definition_in_the_discovering_store() => _other.Projections.HasFor<BoundReadModel>().ShouldBeTrue();
    [Fact] void should_have_the_definition_in_the_adopting_store() => _third.Projections.HasFor<BoundReadModel>().ShouldBeTrue();
    [Fact] void should_have_the_same_projection_identifier() => _third.Projections.GetProjectionIdForModel<BoundReadModel>().ShouldEqual(_other.Projections.GetProjectionIdForModel<BoundReadModel>());
    [Fact] void should_not_share_the_handlers() => _third.Projections.GetAllHandlers().Intersect(_other.Projections.GetAllHandlers()).ShouldBeEmpty();
}
