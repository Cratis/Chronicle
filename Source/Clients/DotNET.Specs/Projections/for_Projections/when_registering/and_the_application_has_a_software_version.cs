// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Projections;
using Microsoft.Extensions.Logging.Abstractions;
using ProtoBuf.Grpc;
using ContractProjections = Cratis.Chronicle.Contracts.Projections.IProjections;

namespace Cratis.Chronicle.Projections.for_Projections.when_registering;

public class and_the_application_has_a_software_version : given.all_dependencies
{
    Projections _projections;
    ContractProjections _contractProjections;

    async Task Establish()
    {
        _eventStore.Name.Returns((EventStoreName)"test-event-store");

        _contractProjections = Substitute.For<ContractProjections>();
        var services = Substitute.For<IServices>();
        services.Projections.Returns(_contractProjections);
        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        ((IChronicleServicesAccessor)connection).Services.Returns(services);
        _eventStore.Connection.Returns(connection);

        _projections = new Projections(
            _eventStore,
            _eventTypes,
            _clientArtifacts,
            _namingPolicy,
            _artifactsActivator,
            _jsonSerializerOptions,
            NullLogger<Projections>.Instance)
        {
            SoftwareVersion = "2.3.4"
        };

        await _projections.Discover();
    }

    async Task Because() => await _projections.Register();

    [Fact] async Task should_send_the_software_version() => await _contractProjections.Received(1).Register(Arg.Is<RegisterRequest>(request => request.Version == "2.3.4"), Arg.Any<CallContext>());
}
