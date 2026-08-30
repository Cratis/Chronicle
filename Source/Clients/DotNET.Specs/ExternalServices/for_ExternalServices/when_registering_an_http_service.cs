// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.ExternalServices;
using Microsoft.Extensions.Logging;
using IExternalServices = Cratis.Chronicle.Contracts.ExternalServices.IExternalServices;

namespace Cratis.Chronicle.Specs.ExternalServices.for_ExternalServices;

public class when_registering_an_http_service : Specification
{
    IChronicleConnection _chronicleConnection;
    IEventStore _eventStore;
    Chronicle.ExternalServices.ExternalServices _externalServices;
    IChronicleServicesAccessor _serviceAccessor;

    void Establish()
    {
        _chronicleConnection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        _serviceAccessor = (IChronicleServicesAccessor)_chronicleConnection;
        _serviceAccessor.Services.Returns(Substitute.For<IServices>());
        _serviceAccessor.Services.ExternalServices.Returns(Substitute.For<IExternalServices>());
        _serviceAccessor.Services.ExternalServices
            .AddExternalServices(Arg.Any<AddExternalServicesRequest>())
            .Returns(Task.FromResult(CommandResult.Success(Guid.NewGuid())));
        _eventStore = Substitute.For<IEventStore>();
        _eventStore.Name.Returns(new EventStoreName("some-event-store"));
        _eventStore.Connection.Returns(_chronicleConnection);
        _externalServices = new Chronicle.ExternalServices.ExternalServices(_eventStore, Substitute.For<ILogger<Chronicle.ExternalServices.ExternalServices>>());
    }

    async Task Because() => await _externalServices.Register(
        "CustomersApi",
        builder => builder
            .Http("https://api.example.com")
            .WithBearerToken("the-token"));

    [Fact]
    void should_add_the_external_service_for_the_event_store() => _serviceAccessor.Services.ExternalServices.Received(1)
        .AddExternalServices(Arg.Is<AddExternalServicesRequest>(_ =>
            _.EventStore == _eventStore.Name &&
            _.ExternalServices.Count() == 1 &&
            _.ExternalServices.First().Name == "CustomersApi" &&
            _.ExternalServices.First().Endpoint.Type == ExternalServiceEndpointType.Http &&
            _.ExternalServices.First().Endpoint.Http!.Url == "https://api.example.com"));
}
