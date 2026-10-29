// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_Projections.when_discovering;

public class and_a_competing_owner_is_added_on_rediscovery : given.projections_with_competing_ids
{
    IProjectionHandler _first;
    Exception _error;

    async Task Establish()
    {
        _clientArtifacts.ModelBoundProjections.Returns([typeof(FirstModel)]);
        await _projections.Discover();
        _first = _projections.GetAllHandlers().Single();
        _clientArtifacts.ModelBoundProjections.Returns([typeof(FirstModel), typeof(SecondModel)]);
    }

    async Task Because() => _error = await Catch.Exception(() => _projections.Discover());

    [Fact] void should_reject_the_competing_owner() => _error.ShouldBeOfExactType<MultipleProjectionsWithSameIdFound>();
    [Fact] void should_leave_the_first_handler_unchanged() => _projections.GetAllHandlers().Single().ShouldEqual(_first);
    [Fact] void should_leave_the_first_definition_unchanged() => _projections.Definitions.Single().Identifier.ShouldEqual(SharedId);
    [Fact] void should_leave_the_first_artifact_unchanged() => _projections.ArtifactRegistrations.Single().ArtifactType.ShouldEqual(typeof(FirstModel));
    [Fact] void should_not_expose_the_second_read_model() => _projections.HasFor<SecondModel>().ShouldBeFalse();
    [Fact] void should_not_register_remotely() => _registrations.ShouldBeEmpty();
}
