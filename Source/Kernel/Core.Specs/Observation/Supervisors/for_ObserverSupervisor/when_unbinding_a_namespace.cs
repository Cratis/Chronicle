// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.Supervisors.for_ObserverSupervisor;

public class when_unbinding_a_namespace : given.a_supervisor
{
    ObserverDurableSubscription? _bound;

    async Task Because()
    {
        await _supervisor.Bind("a-namespace", new(ObserverSubscriptionKind.ClientReactor, false, null, [new("a-connection", "127.0.0.1:11111@1")]));
        await _supervisor.Unbind("a-namespace");
        _bound = await _supervisor.GetBinding("a-namespace");
    }

    [Fact] void should_forget_the_binding() => _bound.ShouldBeNull();
}
