// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_asking_which_layers_the_harness_substitutes;

/// <summary>
/// The pin that keeps the signal worth reading. A Guid-keyed read model with no join, no root removal and no
/// cross-stream child reaches none of the shape-dependent layers, and a report that fired for it too would be
/// noise a consumer learns to ignore — which is the failure mode this whole surface exists to avoid.
/// </summary>
public class and_the_read_model_reaches_none_of_them : Specification
{
    ReadModelScenario<SimpleModule> _scenario;
    IReadOnlyList<ReadModelSubstitution> _substitutions;

    void Establish() => _scenario = new ReadModelScenario<SimpleModule>();

    async Task Because()
    {
        await _scenario.Given.ForEventSource(EventSourceId.New()).Events(new ModuleCreated("My Module"));
        _substitutions = _scenario.Substitutions;
    }

    [Fact] void should_report_nothing() => _substitutions.ShouldBeEmpty();
    [Fact] void should_still_project_the_read_model() => _scenario.Instance!.Name.ShouldEqual("My Module");
}
