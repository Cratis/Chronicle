// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

/// <summary>
/// A scenario exposes two surfaces that look interchangeable and are not: <c>Instances</c> reads what this
/// scenario just materialized, while <c>ReadModels</c> is the production client surface, whose read side wants
/// a running Chronicle. Pinned because the dangerous version of this difference is the quiet one — a
/// <c>ReadModels</c> read that answered "no instances" would read as a passing assertion about a read model
/// the scenario had in fact just built. It refuses instead, and this keeps it refusing.
/// </summary>
public class when_reading_instances_through_the_read_models_surface : Specification
{
    ReadModelScenario<SimpleModule> _scenario;
    EventSourceId _moduleId;
    Exception _error;

    void Establish()
    {
        _scenario = new ReadModelScenario<SimpleModule>();
        _moduleId = EventSourceId.New();
    }

    async Task Because()
    {
        await _scenario.Given.ForEventSource(_moduleId).Events(new ModuleCreated("My Module"));
        _error = await Catch.Exception(async () => await _scenario.ReadModels.GetInstances<SimpleModule>());
    }

    [Fact] void should_materialize_the_instance_in_the_scenario() => _scenario.Instances.Keys.ShouldContain(_moduleId);
    [Fact] void should_refuse_to_read_it_back_through_the_read_models_surface() => _error.ShouldNotBeNull();
    [Fact] void should_name_the_read_model_it_could_not_read() => _error.Message.ShouldContain(nameof(SimpleModule));
    [Fact] void should_say_it_is_not_available_in_a_test_scenario() => _error.Message.ShouldContain("not available in test scenarios");
}
