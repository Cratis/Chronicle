// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;

namespace Cratis.Chronicle.EventSources.for_EventSources.given;

public class all_dependencies : Specification
{
    protected IEventStore _eventStore;
    protected IClientArtifactsProvider _clientArtifacts;
    protected IServices _services;
    protected Contracts.EventSources.IEventSources _eventSourcesService;
    protected Contracts.EventSources.RegisterEventSourcesRequest _request;

    void Establish()
    {
        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        _services = Substitute.For<IServices>();
        _eventSourcesService = Substitute.For<Contracts.EventSources.IEventSources>();
        _eventSourcesService
            .RegisterEventSources(Arg.Do<Contracts.EventSources.RegisterEventSourcesRequest>(request => _request = request))
            .Returns(Task.FromResult(Contracts.Commands.CommandResult.Success(Guid.NewGuid())));
        _services.EventSources.Returns(_eventSourcesService);
        (connection as IChronicleServicesAccessor).Services.Returns(_services);

        _eventStore = Substitute.For<IEventStore>();
        _eventStore.Connection.Returns(connection);
        _eventStore.Name.Returns((EventStoreName)"test-store");

        _clientArtifacts = Substitute.For<IClientArtifactsProvider>();
        _clientArtifacts.EventSources.Returns([]);
    }
}
