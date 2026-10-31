// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Orleans.Concurrency;

namespace Cratis.Chronicle.EventStores.for_EventStoreDeploymentRegistry.when_registering;

public class and_a_newer_deployment_wins_the_first_turn : given.a_registry
{
    DeploymentDecision _older;

    async Task Establish() => await _registry.Register(11UL, "newer");
    async Task Because() => _older = await _registry.Register(10UL, "older");

    [Fact] void should_reject_the_older_deployment() => _older.Accepted.ShouldBeFalse();
    [Fact] void should_report_the_winning_generation() => _older.Stored.ShouldEqual(new DeploymentState(11UL, "newer"));
    [Fact] async Task should_keep_the_winning_state() => (await _registry.Get()).ShouldEqual(new DeploymentState(11UL, "newer"));
    [Fact] async Task should_register_event_types_only_once() => await _eventTypes.Received(1).DiscoverAndRegister(EventStoreName.System);
    [Fact] void should_serialize_competing_turns() => Attribute.IsDefined(typeof(EventStoreDeploymentRegistry), typeof(ReentrantAttribute)).ShouldBeFalse();
}
