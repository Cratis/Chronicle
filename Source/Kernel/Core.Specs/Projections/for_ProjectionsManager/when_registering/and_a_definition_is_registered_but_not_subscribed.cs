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
/// <para>
/// The repair is scheduled rather than awaited. Its cost scales with projections x namespaces and is funnelled
/// through the subscription throttle, so awaiting it inside <see cref="ProjectionsManager.Register"/> put work of
/// unbounded duration inside the caller's response timeout - which is how a large artifact set turned every
/// registration into a timeout and kept the kernel from starting. This proves both halves of that contract:
/// registration returns without subscribing anything itself, and the projection still ends up subscribed once the
/// scheduled pass runs.
/// </para>
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

    bool _subscribedDuringRegistration;

    async Task Because()
    {
        await _grain.Register([_incoming]);
        _subscribedDuringRegistration = _observerGrain.ReceivedCalls().Any();
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_not_subscribe_while_registering() => _subscribedDuringRegistration.ShouldBeFalse();
    [Fact] void should_subscribe_the_projection_that_is_not_subscribed() => _observerGrain.ReceivedCalls().ShouldNotBeEmpty();
    [Fact] void should_not_register_with_the_engine() => _projectionsServiceClient.DidNotReceiveWithAnyArgs().Register(default!, default!);
    [Fact] void should_leave_the_registered_definitions_untouched() => _state.Projections.ShouldContainOnly(_existing);
}
