// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Text.Json;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Timers;
using Microsoft.Extensions.Logging;
using Websocket.Client;
using Websocket.Client.Models;

namespace Aksio.Cratis.Clients.given;

public class a_websocket_connection : Specification
{
    protected WebSocketConnection connection;
    protected Mock<ITimerFactory> timer_factory;
    protected Mock<IExecutionContextManager> execution_context_manager;
    protected Mock<IWebsocketClient> websocket_client;
    protected Mock<IClientLifecycle> client_lifecycle;
    protected Uri endpoint;
    protected MicroserviceId microservice_id;
    protected List<string> messages;
    protected ExecutionContext execution_context;
    protected Subject<ResponseMessage> received;
    protected Subject<ReconnectionInfo> reconnection;
    protected Subject<DisconnectionInfo> disconnection;
    protected Mock<ITimer> timeout_timer;
    protected Mock<ITimer> ping_timer;
    protected TimerCallback timeout_callback;
    protected TimerCallback ping_callback;

    void Establish()
    {
        microservice_id = Guid.NewGuid();

        timer_factory = new();

        timeout_timer = new();
        ping_timer = new();
        timer_factory.Setup(_ => _.Create(IsAny<TimerCallback>(), WebSocketConnection.ConnectTimeout, Timeout.Infinite, null))
            .Returns(
                (TimerCallback callback, int _, int __, object? ___) =>
                {
                    timeout_callback = callback;
                    return timeout_timer.Object;
                });

        timer_factory.Setup(_ => _.Create(IsAny<TimerCallback>(), 0, WebSocketConnection.PingInterval, null))
            .Returns(
                (TimerCallback callback, int _, int __, object? ___) =>
                {
                    ping_callback = callback;
                    return ping_timer.Object;
                });

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
            timer_factory.Object,
            execution_context_manager.Object,
            endpoint,
            client_lifecycle.Object,
            new JsonSerializerOptions(),
            Mock.Of<ILogger<WebSocketConnection>>());
    }
}
