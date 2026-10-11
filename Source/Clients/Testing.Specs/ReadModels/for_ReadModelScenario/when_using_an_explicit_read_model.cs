// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

public class when_using_an_explicit_read_model : Specification
{
    ReadModelScenario<ThingActivity> _scenario;
    Defaults _defaults;

    void Establish()
    {
        var artifacts = new InMemoryClientArtifactsProvider { EventTypes = [typeof(ThingOpened), typeof(ThingTouched)] };
        var options = new ChronicleOptions();
        options.ExplicitArtifacts.RegisterReadModel<ThingActivity>();
        _defaults = new(artifacts, null, options);
        _scenario = new(null, _defaults);
    }

    Task Because() => _scenario.Given.ForEventSource(EventSourceId.New()).Events(new ThingOpened("explicit model"));

    [Fact] void should_project_the_registered_model() => _scenario.Instance!.Name.ShouldEqual("explicit model");
    [Fact] void should_register_the_model_without_catalog_discovery() => _defaults.EventStore.Projections.GetProjectionIdForModel<ThingActivity>().ShouldEqual(new Chronicle.Projections.ProjectionId(typeof(ThingActivity).FullName!));
}
