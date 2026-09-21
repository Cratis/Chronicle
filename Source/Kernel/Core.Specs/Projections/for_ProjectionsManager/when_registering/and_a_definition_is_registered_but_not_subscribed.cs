// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering;

/// <summary>
/// A projection can be registered and stored and yet have no live subscription - the activation-time
/// resubscribe fails for one of them and only logs it. Registration used to decide what to do purely
/// by comparing incoming definitions against stored ones, and a projection that is registered but
/// dark compares equal, so every later registration took the unchanged path and did nothing. The
/// client's own background retry did the same and reported success, while the read model stayed
/// frozen for as long as the activation lived: events kept appending, commands kept returning
/// success, and the observer had no failed partitions because it was observing nothing.
/// </summary>
public class and_a_definition_is_registered_but_not_subscribed : given.a_projections_manager_grain
{
    ProjectionDefinition _existing;
    ProjectionDefinition _incoming;

    void Establish()
    {
        _existing = CreateDefinition("the-projection", "the-read-model");
        _incoming = CreateDefinition("the-projection", "the-read-model");
        _state.Projections = [_existing];
        _readModelDefinitions = [CreateReadModelDefinition("the-read-model")];

        _definitionComparer
            .Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>())
            .Returns(ProjectionDefinitionCompareResult.Same);
    }

    async Task Because() => await _grain.Register([_incoming]);

    [Fact] void should_subscribe_the_projection_that_is_not_subscribed() => _observerGrain.ReceivedCalls().ShouldNotBeEmpty();
    [Fact] void should_not_register_with_the_engine() => _projectionsServiceClient.DidNotReceiveWithAnyArgs().Register(default!, default!);
    [Fact] void should_leave_the_registered_definitions_untouched() => _state.Projections.ShouldContainOnly(_existing);
}
