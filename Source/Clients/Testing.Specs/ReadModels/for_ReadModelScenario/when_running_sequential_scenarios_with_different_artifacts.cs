// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

public class when_running_sequential_scenarios_with_different_artifacts : Specification
{
    PerRunReadModel _firstResult;
    Exception _secondError;
    ReadModelScenario<PerRunReadModel> _second;

    async Task Because()
    {
        var first = new ReadModelScenario<PerRunReadModel>(null, new Defaults(PerRunArtifacts.WithProjection()));
        await first.Given.ForEventSource(EventSourceId.New()).Events(new PerRunRecorded("first"));
        _firstResult = first.Instance!;

        _second = new ReadModelScenario<PerRunReadModel>(null, new Defaults(PerRunArtifacts.WithEvent()));
        await _second.Given.ForEventSource(EventSourceId.New()).Events(new PerRunRecorded("second"));
        _secondError = Catch.Exception(() => _ = _second.Instance);
    }

    [Fact] void should_have_materialized_the_first_run() => _firstResult.ShouldNotBeNull();
    [Fact] void should_not_discover_a_projection_from_the_first_run() => _secondError.ShouldBeOfExactType<NoReadModelHandlerFound>();
}
