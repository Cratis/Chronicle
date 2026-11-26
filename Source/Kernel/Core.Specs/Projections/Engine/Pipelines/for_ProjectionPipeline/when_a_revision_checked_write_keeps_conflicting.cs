// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipeline;

public class when_a_revision_checked_write_keeps_conflicting : given.a_pipeline_whose_write_conflicts
{
    Exception _exception;

    protected override int ConflictsBeforeSucceeding => int.MaxValue;

    async Task Because() => _exception = await Catch.Exception(() => _pipeline.Handle(SomeEvent()));

    [Fact] void should_give_up_with_the_conflict() => _exception.ShouldBeOfExactType<SinkWriteConflictException>();
    [Fact] void should_have_tried_a_bounded_number_of_times() => _step.Changesets.Count.ShouldEqual(8);
}
