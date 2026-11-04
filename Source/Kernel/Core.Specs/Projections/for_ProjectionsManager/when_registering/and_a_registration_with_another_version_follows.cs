// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering;

/// <summary>
/// A registration from a newer version of the client is not a repeat, however soon it follows.
/// </summary>
public class and_a_registration_with_another_version_follows : given.a_projections_manager_grain
{
    const int DefinitionCount = 20;

    void Establish()
    {
        _readModelDefinitions = [CreateReadModelDefinition("the-read-model")];
        _state.Projections = [.. Definitions()];
        _definitionComparer
            .Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>())
            .Returns(ProjectionDefinitionCompareResult.Same);
    }

    async Task Because()
    {
        await _grain.Register(Definitions(), registrant: "the-app", version: "1.0");
        await _grain.Register(Definitions(), registrant: "the-app", version: "2.0");
    }

    [Fact] void should_compare_the_definitions_again() => _definitionComparer.ReceivedWithAnyArgs(DefinitionCount * 2).Compare(default!, default!, default!);

    static ProjectionDefinition[] Definitions() => [.. Enumerable.Range(0, DefinitionCount).Select(index => CreateDefinition($"the-projection-{index}", "the-read-model"))];
}
