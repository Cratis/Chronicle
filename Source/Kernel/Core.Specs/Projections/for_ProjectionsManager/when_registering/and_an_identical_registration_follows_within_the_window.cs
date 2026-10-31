// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering;

/// <summary>
/// After a kernel restart every replica of a client, and each of its retries, registers the same set, and the grain keeps
/// running the ones whose sender has given up. An identical registration that follows one that just completed is
/// answered without asking the store about every definition again.
/// </summary>
public class and_an_identical_registration_follows_within_the_window : given.a_projections_manager_grain
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
        _clock.Advance(TimeSpan.FromSeconds(5));
        await _grain.Register(Definitions(), registrant: "the-app", version: "1.0");
    }

    [Fact] void should_only_compare_the_definitions_for_the_first_registration() => _definitionComparer.ReceivedWithAnyArgs(DefinitionCount).Compare(default!, default!, default!);

    static ProjectionDefinition[] Definitions() => [.. Enumerable.Range(0, DefinitionCount).Select(index => CreateDefinition($"the-projection-{index}", "the-read-model"))];
}
