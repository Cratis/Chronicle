// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_Projections.when_discovering;

public class and_a_projection_removes_children_via_join : given.discovering_with_a_remembering_logger
{
    void Establish()
    {
        _clientArtifacts.Projections.Returns([typeof(TheProjection)]);
        _artifactsActivator
            .ActivateNonDisposable<IProjectionFor<TheReadModel>>(typeof(TheProjection))
            .Returns(new TheProjection());
    }

    async Task Because() => await _projections.Discover();

    [Fact] void should_not_warn() => _logger.Warnings.ShouldBeEmpty();
    [Fact] void should_register_the_projection() => _projections.Definitions.Count.ShouldEqual(1);

    public record TheReadModel(string Id, IEnumerable<Item> Items);

    public class TheProjection : IProjectionFor<TheReadModel>
    {
        public void Define(IProjectionBuilderFor<TheReadModel> builder) =>
            builder.Children(_ => _.Items, children => children.RemovedWithJoin<ChildRemoved>());
    }
}
