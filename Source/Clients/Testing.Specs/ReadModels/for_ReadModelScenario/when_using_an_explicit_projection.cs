// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

public class when_using_an_explicit_projection : Specification
{
    ReadModelScenario<ExplicitNote> _scenario;

    void Establish()
    {
        var artifacts = new InMemoryClientArtifactsProvider { EventTypes = [typeof(ThingTouched)] };
        var options = new ChronicleOptions();
        options.ExplicitArtifacts.RegisterProjection<ExplicitNote>(builder => builder.From<ThingTouched>());
        _scenario = new(null, new Defaults(artifacts, null, options));
    }

    Task Because() => _scenario.Given.ForEventSource(EventSourceId.New()).Events(new ThingTouched("explicit"));

    [Fact] void should_project_the_registered_read_model() => _scenario.Instance!.Note.ShouldEqual("explicit");
}
