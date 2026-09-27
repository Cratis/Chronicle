// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_Projections.when_discovering;

public class and_a_projection_was_added_explicitly : given.projections_for_explicit_registration
{
    void Establish()
    {
        _projections.Add(new DeclarativeExplicitProjection<Inventory>(projection => projection.From<ItemAdded>(), "inventory"));
        _projections.Add(new ModelBoundExplicitProjection(typeof(NotModelBound)));
    }

    Task Because() => _projections.Discover();

    [Fact] void should_build_the_projection_that_can_be_built() => _projections.HasFor<Inventory>().ShouldBeTrue();
    [Fact] void should_hold_its_definition() => _projections.Definitions.Single().Identifier.ShouldEqual("inventory");
    [Fact] void should_report_the_one_that_cannot_be_built() => _projections.ArtifactRegistrations.Single(_ => !_.IsRegistered).Failure.ShouldBeOfExactType<ReadModelIsNotModelBound>();
    [Fact] void should_report_the_one_that_was_built() => _projections.ArtifactRegistrations.Single(_ => _.IsRegistered).ArtifactType.ShouldEqual(typeof(Inventory));
}
