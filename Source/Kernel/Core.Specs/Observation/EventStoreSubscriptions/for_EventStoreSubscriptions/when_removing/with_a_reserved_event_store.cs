// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.EventStoreSubscriptions;
using Grpc.Core;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptions.when_removing;

public class with_a_reserved_event_store : given.an_event_store_subscriptions_service
{
    static readonly string[] _reserved = ["System", "system", "SYSTEM", "x#System"];
    List<Exception> _exceptions;

    void Establish() => _exceptions = [];

    async Task Because()
    {
        foreach (var name in _reserved)
        {
            _exceptions.Add(await Catch.Exception(() => _service.Remove(new RemoveEventStoreSubscriptions { TargetEventStore = name, SubscriptionIds = ["some-subscription"] })));
        }
    }

    [Fact] void should_refuse_every_request() => _exceptions.ShouldEachConformTo(exception => exception is RpcException);
    [Fact] void should_not_call_any_grain() => _grainFactory.ReceivedCalls().ShouldBeEmpty();
}
