// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipeline;

public class when_a_write_without_revision_checks_conflicts : given.a_pipeline_whose_write_conflicts
{
    Exception _exception;

    protected override bool SupportsFencedGlobalWrites => false;

    async Task Because() => _exception = await Catch.Exception(() => _pipeline.Handle(SomeEvent()));

    [Fact] void should_fail_with_the_conflict() => _exception.ShouldBeOfExactType<SinkWriteConflictException>();
    [Fact] void should_not_retry() => _step.Changesets.Count.ShouldEqual(1);
}
