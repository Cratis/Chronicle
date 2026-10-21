// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_Projections.when_discovering;

public class and_a_projection_removes_its_root_via_join : given.discovering_with_a_remembering_logger
{
    async Task Establish()
    {
        _clientArtifacts.Projections.Returns([typeof(TheProjection)]);
        _artifactsActivator
            .ActivateNonDisposable<IProjectionFor<TheReadModel>>(typeof(TheProjection))
            .Returns(new TheProjection());
        await Task.CompletedTask;
    }

    async Task Because() => await _projections.Discover();

    [Fact] void should_warn_once() => _logger.Warnings.Count.ShouldEqual(1);
    [Fact] void should_name_the_read_model() => _logger.Warnings[0].ShouldContain(nameof(TheReadModel));
    [Fact] void should_still_register_the_projection() => _projections.Definitions.Count.ShouldEqual(1);

    [Fact] void should_not_change_the_definition_sent_to_the_kernel() => _projections.Definitions.Single().RemovedWithJoin.ShouldBeEmpty();

    public record TheReadModel(string Id);

    public class TheProjection : IProjectionFor<TheReadModel>
    {
        public void Define(IProjectionBuilderFor<TheReadModel> builder) => builder.RemovedWithJoin<RootRemoved>();
    }
}
