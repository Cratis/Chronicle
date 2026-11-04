// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering;

/// <summary>
/// The shortcut only covers a registration that follows another closely. One that arrives after the window - a client
/// reconnecting well after the first - is wanted again and runs in full.
/// </summary>
public class and_an_identical_registration_follows_after_the_window : given.a_projections_manager_grain
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
        _clock.Advance(ProjectionsManager.IdenticalRegistrationWindow + TimeSpan.FromSeconds(1));
        await _grain.Register(Definitions(), registrant: "the-app", version: "1.0");
    }

    [Fact] void should_compare_the_definitions_again() => _definitionComparer.ReceivedWithAnyArgs(DefinitionCount * 2).Compare(default!, default!, default!);

    static ProjectionDefinition[] Definitions() => [.. Enumerable.Range(0, DefinitionCount).Select(index => CreateDefinition($"the-projection-{index}", "the-read-model"))];
}
