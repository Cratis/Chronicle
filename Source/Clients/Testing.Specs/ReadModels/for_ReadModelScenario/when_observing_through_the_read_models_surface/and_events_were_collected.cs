// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_observing_through_the_read_models_surface;

public class and_events_were_collected : Specification
{
    ReadModelScenario<SimpleModule> _scenario;
    Exception _error;

    async Task Establish()
    {
        _scenario = new();
        await _scenario.Given.ForEventSource(EventSourceId.New()).Events(new ModuleCreated("First"));
    }

    void Because() => _error = Catch.Exception(() => _scenario.ReadModels.Observe<SimpleModule>());
    void Destroy() => _scenario.Dispose();

    [Fact] void should_keep_the_scenario_observation_unsupported() => _error.ShouldBeOfExactType<ReadModelCannotBeObserved>();
}
