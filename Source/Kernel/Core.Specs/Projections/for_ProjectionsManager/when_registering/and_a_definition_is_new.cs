// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering;

public class and_a_definition_is_new : given.a_projections_manager_grain
{
    ProjectionDefinition _incoming;

    void Establish()
    {
        _incoming = CreateDefinition("the-projection", "the-read-model");
        _readModelDefinitions = [CreateReadModelDefinition("the-read-model")];
    }

    async Task Because() => await _grain.Register([_incoming]);

    [Fact] void should_not_consult_the_comparer() => _definitionComparer.DidNotReceiveWithAnyArgs().Compare(default!, default!, default!);

    [Fact]
    void should_register_the_definition_with_the_engine() =>
        _projectionsServiceClient.Received(1).Register(
            (EventStoreName)EventStore,
            Arg.Is<IEnumerable<ProjectionDefinition>>(definitions => definitions.SequenceEqual(new[] { _incoming })));

    [Fact] void should_set_the_definition_on_the_projection_grain() => _projectionGrain.Received(1).SetDefinition(_incoming);
    [Fact] void should_add_the_definition_to_the_registered_ones() => _state.Projections.ShouldContainOnly(_incoming);
}
