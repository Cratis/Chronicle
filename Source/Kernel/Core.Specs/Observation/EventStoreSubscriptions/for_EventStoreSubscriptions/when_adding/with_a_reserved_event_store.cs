// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.EventStoreSubscriptions;
using Grpc.Core;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptions.when_adding;

public class with_a_reserved_event_store : given.an_event_store_subscriptions_service
{
    static readonly string[] _reserved = ["System", "system", "SYSTEM", "x#System"];
    List<Exception> _exceptions;

    void Establish() => _exceptions = [];

    async Task Because()
    {
        foreach (var name in _reserved)
        {
            _exceptions.Add(await Catch.Exception(() => _service.Add(RequestFor("target-event-store", name))));
            _exceptions.Add(await Catch.Exception(() => _service.Add(RequestFor(name, "source-event-store"))));
        }
    }

    [Fact] void should_refuse_every_request() => _exceptions.ShouldEachConformTo(exception => exception is RpcException);
    [Fact] void should_refuse_with_invalid_argument() => _exceptions.Cast<RpcException>().ShouldEachConformTo(exception => exception.StatusCode == StatusCode.InvalidArgument);
    [Fact] void should_not_call_any_grain() => _grainFactory.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_read_any_storage() => _storage.ReceivedCalls().ShouldBeEmpty();

    static AddEventStoreSubscriptions RequestFor(string target, string source) => new()
    {
        TargetEventStore = target,
        Subscriptions = [new EventStoreSubscriptionDefinition { Identifier = "some-subscription", SourceEventStore = source, EventTypes = [] }]
    };
}
