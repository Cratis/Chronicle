// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.AspNetCore.Diagnostics.for_ChronicleHealthCheck.given;

public class a_health_check : Specification
{
    protected ChronicleHealthCheck _healthCheck;
    protected IChronicleClient _client;
    protected IEventStore _eventStore;
    protected IChronicleConnection _connection;
    protected IConnectionLifecycle _lifecycle;
    protected HealthCheckContext _context;

    void Establish()
    {
        _lifecycle = Substitute.For<IConnectionLifecycle>();
        _connection = Substitute.For<IChronicleConnection>();
        _connection.Lifecycle.Returns(_lifecycle);
        _eventStore = Substitute.For<IEventStore>();
        _eventStore.Connection.Returns(_connection);
        _client = Substitute.For<IChronicleClient>();
        _client.GetEventStore(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>()).Returns(_eventStore);

        _context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration(ChronicleHealthCheck.Name, Substitute.For<IHealthCheck>(), null, null)
        };

        _healthCheck = new ChronicleHealthCheck(
            _client,
            Options.Create(new ChronicleAspNetCoreOptions { EventStore = "the-store" }));
    }
}
