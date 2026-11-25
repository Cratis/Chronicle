// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.Supervisors.for_ObserverSupervisor;

public class when_binding_a_namespace : given.a_supervisor
{
    readonly ObserverDurableSubscription _binding = new(ObserverSubscriptionKind.ClientReactor, false, null, [new("a-connection", "127.0.0.1:11111@1")]);
    ObserverDurableSubscription? _bound;
    ObserverDurableSubscription? _other;

    async Task Because()
    {
        await _supervisor.Bind("a-namespace", _binding);
        _bound = await _supervisor.GetBinding("a-namespace");
        _other = await _supervisor.GetBinding("another-namespace");
    }

    [Fact] void should_answer_with_the_binding_of_the_namespace() => _bound.ShouldEqual(_binding);
    [Fact] void should_know_no_binding_for_another_namespace() => _other.ShouldBeNull();
}
