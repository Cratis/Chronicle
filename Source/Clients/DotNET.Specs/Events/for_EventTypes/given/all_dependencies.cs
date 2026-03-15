// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Events.Migrations;
using Cratis.Chronicle.Schemas;
using NJsonSchema;

namespace Cratis.Chronicle.Events.for_EventTypes.given;

public class all_dependencies : Specification
{
    protected IEventStore _eventStore;
    protected IJsonSchemaGenerator _schemaGenerator;
    protected IClientArtifactsProvider _clientArtifacts;
    protected IEventTypeMigrators _eventTypeMigrators;
    protected IChronicleServicesAccessor _servicesAccessor;
    protected IServices _services;
    protected Contracts.Events.IEventTypes _eventTypesService;

    void Establish()
    {
        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        _servicesAccessor = connection as IChronicleServicesAccessor;
        _services = Substitute.For<IServices>();
        _eventTypesService = Substitute.For<Contracts.Events.IEventTypes>();
        _services.EventTypes.Returns(_eventTypesService);
        _servicesAccessor.Services.Returns(_services);

        _eventStore = Substitute.For<IEventStore>();
        _eventStore.Connection.Returns(connection);
        _eventStore.Name.Returns((EventStoreName)"test-store");

        _schemaGenerator = Substitute.For<IJsonSchemaGenerator>();
        _clientArtifacts = Substitute.For<IClientArtifactsProvider>();
        _eventTypeMigrators = Substitute.For<IEventTypeMigrators>();

        _clientArtifacts.EventTypes.Returns([]);
        _eventTypeMigrators.GetMigratorsFor(Arg.Any<Type>()).Returns([]);
        _schemaGenerator.Generate(Arg.Any<Type>()).Returns(new JsonSchema());
    }
}
