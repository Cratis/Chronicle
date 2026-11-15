// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering;

/// <summary>
/// A namespace that appears between two identical registrations is work the second one owes: the projections still
/// have to be subscribed there, so it is not answered as a repeat.
/// </summary>
public class and_a_namespace_was_added_between_identical_registrations : given.a_projections_manager_grain
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
        _namespaces.GetAll().Returns([EventStoreNamespaceName.Default, new EventStoreNamespaceName("another")]);
        await _grain.Register(Definitions(), registrant: "the-app", version: "1.0");
    }

    [Fact] void should_compare_the_definitions_again() => _definitionComparer.ReceivedWithAnyArgs(DefinitionCount * 2).Compare(default!, default!, default!);

    static ProjectionDefinition[] Definitions() => [.. Enumerable.Range(0, DefinitionCount).Select(index => CreateDefinition($"the-projection-{index}", "the-read-model"))];
}
