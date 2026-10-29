// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_Projections.when_registering_explicitly;

public class with_an_unbuildable_pending_recipe : given.projections_with_competing_ids
{
    IProjectionHandler _handler;

    void Establish() => _projections.Add(new DeclarativeExplicitProjection<FirstModel>(_ => throw new InvalidOperationException("Cannot build pending recipe"), SharedId));

    async Task Because()
    {
        _handler = await _projections.Register<SecondModel>(projection => projection.From<CustomerRegistered>(), SharedId);
        await _projections.Discover();
    }

    [Fact] void should_allow_the_valid_owner() => _handler.Id.ShouldEqual((ProjectionId)SharedId);
    [Fact] void should_not_publish_the_unbuildable_owner() => _projections.HasFor<FirstModel>().ShouldBeFalse();
    [Fact] void should_keep_the_valid_owner_after_discovery() => _projections.GetProjectionIdForModel<SecondModel>().ShouldEqual((ProjectionId)SharedId);
    [Fact] void should_report_the_pending_recipe_failure_at_discovery() => _projections.ArtifactRegistrations.Single(_ => _.ArtifactType == typeof(FirstModel)).Failure.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_only_publish_the_valid_definition() => _projections.Definitions.Single().Identifier.ShouldEqual(SharedId);
}
