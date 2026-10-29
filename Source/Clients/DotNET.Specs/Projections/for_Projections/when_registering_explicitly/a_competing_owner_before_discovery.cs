// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_Projections.when_registering_explicitly;

public class a_competing_owner_before_discovery : given.projections_with_competing_ids
{
    Exception _error;
    int _handlersBeforeDiscovery;
    int _definitionsBeforeDiscovery;
    int _artifactsBeforeDiscovery;

    void Establish()
    {
        _lifecycle.IsConnected.Returns(true);
        _projections.Add(new DeclarativeExplicitProjection<FirstModel>(projection => projection.From<CustomerRegistered>(), SharedId));
    }

    async Task Because()
    {
        _error = await Catch.Exception(() => _projections.Register<SecondModel>(projection => projection.From<CustomerRegistered>(), SharedId));
        _handlersBeforeDiscovery = _projections.GetAllHandlers().Count();
        _definitionsBeforeDiscovery = _projections.Definitions.Count;
        _artifactsBeforeDiscovery = _projections.ArtifactRegistrations.Count;
        await _projections.Discover();
    }

    [Fact] void should_reject_the_competing_owner() => _error.ShouldBeOfExactType<MultipleProjectionsWithSameIdFound>();
    [Fact] void should_identify_the_pending_owner() => _error.Message.ShouldContain(typeof(FirstModel).FullName);
    [Fact] void should_identify_the_candidate_owner() => _error.Message.ShouldContain(typeof(SecondModel).FullName);
    [Fact] void should_not_publish_handlers_before_discovery() => _handlersBeforeDiscovery.ShouldEqual(0);
    [Fact] void should_not_publish_definitions_before_discovery() => _definitionsBeforeDiscovery.ShouldEqual(0);
    [Fact] void should_not_publish_artifact_outcomes_before_discovery() => _artifactsBeforeDiscovery.ShouldEqual(0);
    [Fact] void should_keep_the_pending_owner_after_discovery() => _projections.GetProjectionIdForModel<FirstModel>().ShouldEqual((ProjectionId)SharedId);
    [Fact] void should_not_retain_the_rejected_owner() => _projections.HasFor<SecondModel>().ShouldBeFalse();
    [Fact] void should_only_discover_the_original_definition() => _projections.Definitions.Single().Identifier.ShouldEqual(SharedId);
    [Fact] void should_not_register_projections_remotely() => _registrations.ShouldBeEmpty();
    [Fact] void should_not_register_the_rejected_read_model_remotely() => _readModels.DidNotReceive().Register<SecondModel>();
}
