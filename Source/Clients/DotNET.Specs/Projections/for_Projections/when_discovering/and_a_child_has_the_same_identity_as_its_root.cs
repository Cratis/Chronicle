// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Projections.for_Projections.when_discovering;

public class and_a_child_has_the_same_identity_as_its_root : given.projections_for_explicit_registration
{
    void Establish() => _clientArtifacts.ModelBoundProjections.Returns([typeof(Root), typeof(Child)]);

    Task Because() => _projections.Discover();

    [Fact] void should_only_publish_the_effective_root() => _projections.Definitions.Single().Identifier.ShouldEqual("root-id");
    [Fact] void should_not_treat_a_child_as_a_competing_owner() => _projections.GetAllHandlers().Single().ReadModelType.ShouldEqual(typeof(Root));
    [Fact] void should_keep_the_child_mapping() => _projections.Definitions.Single().Children.ShouldNotBeEmpty();

    [Projection("root-id")]
    [FromEvent<CustomerRegistered>]
    public record Root([ChildrenFrom<CustomerRegistered>] IEnumerable<Child> Children);

    [Projection("root-id")]
    [FromEvent<CustomerRegistered>]
    public record Child(string Name);
}
