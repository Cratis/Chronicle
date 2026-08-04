// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_strict_fidelity_is_enabled;

/// <summary>
/// The other half of the ratchet: opting in must not cost a suite the specs it already has. A read model that
/// reaches no substituted layer keeps projecting exactly as before, so strict mode can be turned on across a
/// whole suite and only the shapes that genuinely need a kernel-backed sibling change color.
/// </summary>
public class and_the_read_model_reaches_none_of_them : Specification
{
    ReadModelScenario<SimpleModule> _scenario;
    EventSourceId _moduleId;
    Exception _error;

    void Establish()
    {
        _scenario = new ReadModelScenario<SimpleModule>().WithStrictFidelity();
        _moduleId = EventSourceId.New();
    }

    async Task Because()
    {
        await _scenario.Given.ForEventSource(_moduleId).Events(new ModuleCreated("My Module"));
        _error = Catch.Exception(() => _ = _scenario.Instance);
    }

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_still_project_the_read_model() => _scenario.Instance!.Name.ShouldEqual("My Module");
}
