// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing.EventSequences;
using Cratis.Chronicle.Testing.ReadModels;

namespace Cratis.Chronicle.Testing.for_InMemoryClientArtifactsProvider;

public class when_using_a_per_run_catalog : Specification
{
    EventScenario _scenario;
    AppendResult _result;

    void Establish()
    {
        var artifacts = new InMemoryClientArtifactsProvider { EventTypes = [typeof(ThingTouched)] };
        _scenario = new(new Defaults(artifacts));
    }

    async Task Because() => _result = await _scenario.EventLog.Append(EventSourceId.New(), new ThingTouched("catalog"));

    [Fact] void should_append_an_event_from_the_catalog() => _result.IsSuccess.ShouldBeTrue();

    void Destroy() => _scenario.Dispose();
}
