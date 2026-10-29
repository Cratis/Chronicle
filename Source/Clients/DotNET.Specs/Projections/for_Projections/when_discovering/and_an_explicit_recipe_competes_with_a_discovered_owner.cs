// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_Projections.when_discovering;

public class and_an_explicit_recipe_competes_with_a_discovered_owner : given.projections_with_competing_ids
{
    Exception _error;

    void Establish()
    {
        _clientArtifacts.ModelBoundProjections.Returns([typeof(FirstModel)]);
        _projections.Add(new ModelBoundExplicitProjection(typeof(SecondModel)));
    }

    async Task Because() => _error = await Catch.Exception(() => _projections.Discover());

    [Fact] void should_reject_the_competing_recipe() => _error.ShouldBeOfExactType<MultipleProjectionsWithSameIdFound>();
    [Fact] void should_identify_the_id() => _error.Message.ShouldContain(SharedId);
    [Fact] void should_identify_the_discovered_owner() => _error.Message.ShouldContain(typeof(FirstModel).FullName);
    [Fact] void should_identify_the_explicit_owner() => _error.Message.ShouldContain(typeof(SecondModel).FullName);
    [Fact] void should_not_publish_any_handlers() => _projections.GetAllHandlers().ShouldBeEmpty();
    [Fact] void should_not_publish_any_definitions() => _projections.Definitions.ShouldBeEmpty();
    [Fact] void should_not_publish_any_artifact_registrations() => _projections.ArtifactRegistrations.ShouldBeEmpty();
    [Fact] void should_not_register_remotely() => _registrations.ShouldBeEmpty();
}
