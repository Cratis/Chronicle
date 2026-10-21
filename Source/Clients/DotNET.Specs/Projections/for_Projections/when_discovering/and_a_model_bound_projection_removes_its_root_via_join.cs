// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Projections.for_Projections.when_discovering;

public class and_a_model_bound_projection_removes_its_root_via_join : given.discovering_with_a_remembering_logger
{
    void Establish() => _clientArtifacts.ModelBoundProjections.Returns([typeof(TheReadModel)]);

    async Task Because() => await _projections.Discover();

    [Fact] void should_warn_once() => _logger.Warnings.Count.ShouldEqual(1);
    [Fact] void should_name_the_read_model() => _logger.Warnings[0].ShouldContain(nameof(TheReadModel));
    [Fact] void should_still_register_the_projection() => _projections.Definitions.Count.ShouldEqual(1);

    [RemovedWithJoin<RootRemoved>]
    public record TheReadModel(string Id);
}
