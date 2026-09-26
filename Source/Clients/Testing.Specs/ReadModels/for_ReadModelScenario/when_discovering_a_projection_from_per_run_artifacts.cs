// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

public class when_discovering_a_projection_from_per_run_artifacts : Specification
{
    ReadModelScenario<PerRunReadModel> _scenario;
    PerRunReadModel _result;

    void Establish() => _scenario = new ReadModelScenario<PerRunReadModel>(null, new Defaults(PerRunArtifacts.WithProjection()));

    async Task Because()
    {
        await _scenario.Given.ForEventSource(EventSourceId.New()).Events(new PerRunRecorded("registered"));
        _result = _scenario.Instance!;
    }

    [Fact] void should_use_the_supplied_projection() => _result.Second.ShouldEqual("registered");
}
