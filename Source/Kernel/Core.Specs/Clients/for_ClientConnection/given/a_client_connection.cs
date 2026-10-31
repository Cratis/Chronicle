// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Clients.for_ClientConnection.given;

public class a_client_connection : Specification
{
    protected ClientConnection _connection;
    protected IGrainFactory _grainFactory;
    protected GrainId _first = GrainId.Create("reactor", "first");
    protected GrainId _second = GrainId.Create("reducer", "second");
    protected IClientDisconnectionSubscriber _firstSubscriber;
    protected IClientDisconnectionSubscriber _secondSubscriber;

    void Establish()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        _firstSubscriber = Substitute.For<IClientDisconnectionSubscriber>();
        _secondSubscriber = Substitute.For<IClientDisconnectionSubscriber>();
        _grainFactory.GetGrain<IClientDisconnectionSubscriber>(_first).Returns(_firstSubscriber);
        _grainFactory.GetGrain<IClientDisconnectionSubscriber>(_second).Returns(_secondSubscriber);
        _connection = new ClientConnection(_grainFactory, Substitute.For<ILogger<ClientConnection>>());
    }
}
