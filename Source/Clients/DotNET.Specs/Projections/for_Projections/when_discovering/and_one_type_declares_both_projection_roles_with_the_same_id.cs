// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Projections.for_Projections.when_discovering;

public class and_one_type_declares_both_projection_roles_with_the_same_id : given.projections_for_explicit_registration
{
    Exception _error;

    void Establish()
    {
        _clientArtifacts.Projections.Returns([typeof(ModelAndProjection)]);
        _clientArtifacts.ModelBoundProjections.Returns([typeof(ModelAndProjection)]);
        _artifactsActivator.ActivateNonDisposable<IProjectionFor<ModelAndProjection>>(typeof(ModelAndProjection)).Returns(new ModelAndProjection());
    }

    async Task Because() => _error = await Catch.Exception(() => _projections.Discover());

    [Fact] void should_not_consider_the_fluent_role_a_model_bound_overlay() => _error.ShouldBeOfExactType<MultipleProjectionsWithSameIdFound>();
    [Fact] void should_identify_the_owner() => _error.Message.ShouldContain(typeof(ModelAndProjection).FullName);
    [Fact] void should_not_publish_handlers() => _projections.GetAllHandlers().ShouldBeEmpty();
    [Fact] void should_not_register_remotely() => _registrations.ShouldBeEmpty();

    [Projection("shared-id")]
    [NotRewindable]
    public class ModelAndProjection : IProjectionFor<ModelAndProjection>
    {
        public void Define(IProjectionBuilderFor<ModelAndProjection> builder)
        {
        }
    }
}
