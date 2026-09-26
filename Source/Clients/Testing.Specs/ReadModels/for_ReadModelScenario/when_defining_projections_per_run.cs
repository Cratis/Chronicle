// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

public class when_defining_projections_per_run : Specification
{
    ReadModelScenario<PerRunModel> _first;
    ReadModelScenario<PerRunModel> _second;

    void Establish()
    {
        var firstArtifacts = Substitute.For<IClientArtifactsProvider>();
        firstArtifacts.EventTypes.Returns([typeof(PerRunEvent)]);
        var secondArtifacts = Substitute.For<IClientArtifactsProvider>();
        secondArtifacts.EventTypes.Returns([typeof(PerRunEvent)]);

        _first = new ReadModelScenario<PerRunModel>(null, new Defaults(firstArtifacts))
            .WithProjection(builder => builder.From<PerRunEvent>(from => from.Set(model => model.First).To(@event => @event.Value)));
        _second = new ReadModelScenario<PerRunModel>(null, new Defaults(secondArtifacts))
            .WithProjection(builder => builder.From<PerRunEvent>(from => from.Set(model => model.Second).To(@event => @event.Value)));
    }

    async Task Because()
    {
        var id = EventSourceId.New();
        await _first.Given.ForEventSource(id).Events(new PerRunEvent("value"));
        await _second.Given.ForEventSource(id).Events(new PerRunEvent("value"));
    }

    [Fact] void should_apply_the_first_definition_only_to_the_first_scenario() => _first.Instance!.First.ShouldEqual("value");
    [Fact] void should_not_apply_the_second_definition_to_the_first_scenario() => _first.Instance!.Second.ShouldBeNull();
    [Fact] void should_apply_the_second_definition_only_to_the_second_scenario() => _second.Instance!.Second.ShouldEqual("value");
    [Fact] void should_not_apply_the_first_definition_to_the_second_scenario() => _second.Instance!.First.ShouldBeNull();
}
