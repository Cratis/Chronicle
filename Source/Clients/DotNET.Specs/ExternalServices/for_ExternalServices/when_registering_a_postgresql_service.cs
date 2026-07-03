// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.ExternalServices;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ExternalServices;
using Microsoft.Extensions.Logging;
using IExternalServices = Cratis.Chronicle.Contracts.ExternalServices.IExternalServices;

namespace Cratis.Chronicle.Specs.ExternalServices.for_ExternalServices;

public class when_registering_a_postgresql_service : Specification
{
    IChronicleConnection _chronicleConnection;
    IEventStore _eventStore;
    Chronicle.ExternalServices.ExternalServices _externalServices;
    IChronicleServicesAccessor _serviceAccessor;

    void Establish()
    {
        _chronicleConnection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        _serviceAccessor = _chronicleConnection as IChronicleServicesAccessor;
        _serviceAccessor.Services.Returns(Substitute.For<IServices>());
        _serviceAccessor.Services.ExternalServices.Returns(Substitute.For<IExternalServices>());
        _eventStore = Substitute.For<IEventStore>();
        _eventStore.Name.Returns(new EventStoreName("some-event-store"));
        _eventStore.Connection.Returns(_chronicleConnection);
        _externalServices = new Chronicle.ExternalServices.ExternalServices(_eventStore, Substitute.For<ILogger<Chronicle.ExternalServices.ExternalServices>>());
    }

    async Task Because() => await _externalServices.Register(
        "CustomersDb",
        builder => builder
            .PostgreSql("db.example.com", "customers", "postgres", "secret", port: 5432));

    [Fact]
    void should_configure_a_postgresql_database_endpoint() => _serviceAccessor.Services.ExternalServices.Received(1)
        .Add(Arg.Is<AddExternalServices>(_ =>
            _.ExternalServices[0].Endpoint.Type == ExternalServiceEndpointType.PostgreSql &&
            _.ExternalServices[0].Endpoint.Database!.Host == "db.example.com" &&
            _.ExternalServices[0].Endpoint.Database!.Port == 5432 &&
            _.ExternalServices[0].Endpoint.Database!.Database == "customers"));
}
