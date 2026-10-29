// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_Projections.when_discovering;

public class and_a_stable_model_bound_id_is_registered_explicitly : given.projections_with_competing_ids
{
    IProjectionHandler _first;
    IProjectionHandler _second;

    void Establish()
    {
        _clientArtifacts.ModelBoundProjections.Returns([typeof(FirstModel)]);
        _projections.Add(new ModelBoundExplicitProjection(typeof(FirstModel)));
    }

    async Task Because()
    {
        await _projections.Discover();
        _first = await _projections.Register<FirstModel>();
        _second = await _projections.Register<FirstModel>();
    }

    [Fact] void should_keep_one_definition() => _projections.Definitions.Single().Identifier.ShouldEqual(SharedId);
    [Fact] void should_keep_one_handler() => _projections.GetAllHandlers().Single().ShouldEqual(_first);
    [Fact] void should_keep_repeated_registration_idempotent() => _second.ShouldEqual(_first);
    [Fact] void should_resolve_the_same_id_by_read_model() => _projections.GetProjectionIdForModel<FirstModel>().ShouldEqual((ProjectionId)SharedId);
    [Fact] void should_keep_one_successful_artifact() => _projections.ArtifactRegistrations.Single().ArtifactType.ShouldEqual(typeof(FirstModel));
    [Fact] void should_not_register_remotely_while_disconnected() => _registrations.ShouldBeEmpty();
}
