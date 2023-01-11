// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Text.Json;
using Aksio.Cratis.Execution;
using Microsoft.Extensions.Logging;
using Websocket.Client;
using Websocket.Client.Models;

namespace Aksio.Cratis.Clients.given;

public class a_websocket_connection : Specification
{
    protected WebSocketConnection connection;
    protected Mock<IExecutionContextManager> execution_context_manager;
    protected Mock<IWebsocketClient> websocket_client;
    protected Mock<IClientLifecycle> client_lifecycle;
    protected Uri endpoint;
    protected MicroserviceId microservice_id;
    protected List<string> messages;
    protected ExecutionContext execution_context;
    protected ReplaySubject<ResponseMessage> received;
    protected ReplaySubject<ReconnectionInfo> reconnection;
    protected ReplaySubject<DisconnectionInfo> disconnection;

    void Establish()
    {

        microservice_id = Guid.NewGuid();
        execution_context_manager = new();
        execution_context = new ExecutionContext(microservice_id, TenantId.Development, CorrelationId.New(), CausationId.System, CausedBy.System);
        execution_context_manager.Setup(_ => _.Current).Returns(execution_context);

        messages = new();
        websocket_client = new();
        websocket_client.Setup(_ => _.Send(IsAny<string>())).Callback(messages.Add);

        received = new();
        websocket_client.SetupGet(_ => _.MessageReceived).Returns(received);

        reconnection = new();
        websocket_client.SetupGet(_ => _.ReconnectionHappened).Returns(reconnection);

        disconnection = new();
        websocket_client.SetupGet(_ => _.DisconnectionHappened).Returns(disconnection);

        endpoint = new("http://localhost:8080");
        client_lifecycle = new();
        connection = new WebSocketConnection(
            websocket_client.Object,
            execution_context_manager.Object,
            endpoint,
            client_lifecycle.Object,
            new JsonSerializerOptions(),
            Mock.Of<ILogger<WebSocketConnection>>());
    }
}
