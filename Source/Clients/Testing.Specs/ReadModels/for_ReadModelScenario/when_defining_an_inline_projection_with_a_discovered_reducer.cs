// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

public class when_defining_an_inline_projection_with_a_discovered_reducer : Specification
{
    ReadModelScenario<PerRunModel> _scenario;

    void Establish()
    {
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.EventTypes.Returns([typeof(PerRunEvent)]);
        artifacts.Reducers.Returns([typeof(PerRunModelReducer)]);
        _scenario = new ReadModelScenario<PerRunModel>(null, new Defaults(artifacts))
            .WithProjection(builder => builder.From<PerRunEvent>(from => from.Set(model => model.First).To(@event => @event.Value)));
    }

    async Task Because() => await _scenario.Given.ForEventSource(EventSourceId.New()).Events(new PerRunEvent("projected"));

    [Fact] void should_use_the_inline_projection() => _scenario.Instance!.First.ShouldEqual("projected");
    [Fact] void should_not_run_the_discovered_reducer() => _scenario.Instance!.Second.ShouldBeNull();
}
