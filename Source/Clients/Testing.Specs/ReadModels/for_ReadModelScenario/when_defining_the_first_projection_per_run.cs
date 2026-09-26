// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

public class when_defining_the_first_projection_per_run : Specification
{
    ReadModelScenario<PerRunReadModel> _scenario;
    PerRunReadModel _result;

    void Establish() => _scenario = new ReadModelScenario<PerRunReadModel>(null, new Defaults(PerRunArtifacts.WithEvent()))
        .WithProjection(builder => builder.From<PerRunRecorded>(from => from.Set(model => model.First).To(e => e.Value)));

    async Task Because()
    {
        await _scenario.Given.ForEventSource(EventSourceId.New()).Events(new PerRunRecorded("first"));
        _result = _scenario.Instance!;
    }

    [Fact] void should_seed_from_the_supplied_artifacts() => _result.ShouldNotBeNull();
    [Fact] void should_map_only_the_first_field() => _result.First.ShouldEqual("first");
    [Fact] void should_not_map_the_second_field() => _result.Second.ShouldBeNull();
}
