// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Events;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Specs.EventStoreSubscriptions.for_EventStoreSubscriptions.given;

public class subscriptions : Specification
{
    protected Chronicle.EventStoreSubscriptions.EventStoreSubscriptions _subscriptions;
    protected Contracts.Observation.EventStoreSubscriptions.IEventStoreSubscriptions _service;
    protected Contracts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition _request;
    protected IEnumerable<Chronicle.EventStoreSubscriptions.EventStoreSubscriptionDefinition> _definitions;

    void Establish()
    {
        var types = Substitute.For<IEventTypes>();
        types.All.Returns(ImmutableList.Create(new EventType("customer", 3)));
        types.GetEventTypeFor(typeof(CustomerRegistered)).Returns(new EventType("customer", 3));
        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        var accessor = (IChronicleServicesAccessor)connection;
        var services = Substitute.For<IServices>();
        _service = Substitute.For<Contracts.Observation.EventStoreSubscriptions.IEventStoreSubscriptions>();
        services.EventStoreSubscriptions.Returns(_service);
        accessor.Services.Returns(services);
        var store = Substitute.For<IEventStore>();
        store.Name.Returns(new EventStoreName("target"));
        store.Connection.Returns(connection);
        _service.Add(Arg.Any<AddEventStoreSubscriptions>()).Returns(call =>
        {
            _request = call.Arg<AddEventStoreSubscriptions>().Subscriptions.Single();
            return Task.CompletedTask;
        });
        _subscriptions = new(types, store, Substitute.For<ILogger<Chronicle.EventStoreSubscriptions.EventStoreSubscriptions>>());
    }

    protected record CustomerRegistered;
}
