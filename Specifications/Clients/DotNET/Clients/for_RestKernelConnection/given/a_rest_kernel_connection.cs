// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Aksio.Commands;
using Aksio.Cratis.Client;
using Aksio.Cratis.Configuration;
using Aksio.Cratis.Connections;
using Aksio.Tasks;
using Aksio.Timers;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aksio.Cratis.Clients.for_RestKernelConnection.given;

public class a_rest_kernel_connection : Specification
{
    protected string connect_route;
    protected string ping_route;

    protected HttpResponseMessage success_message => new(HttpStatusCode.OK)
    {
        Content = JsonContent.Create(CommandResult.Success)
    };

    protected HttpResponseMessage failed_message => new(HttpStatusCode.OK)
    {
        Content = JsonContent.Create(new CommandResult { ExceptionMessages = new[] { "Something went wrong" } })
    };

    protected HttpResponseMessage not_found_message => new(HttpStatusCode.NotFound);
    protected Mock<IOptions<ClientOptions>> options;
    protected ClientOptions options_instance;
    protected Mock<IServer> server;
    protected Mock<IServiceProvider> service_provider;
    protected Mock<IFeatureCollection> features;
    protected Mock<IServerAddressesFeature> server_addresses;
    protected Mock<ITaskFactory> task_factory;
    protected Mock<ITimerFactory> timer_factory;
    protected Mock<IExecutionContextManager> execution_context_manager;
    protected Mock<IConnectionLifecycle> connection_lifecycle;
    protected Mock<IClient> client;
    protected KernelConnection connection;
    protected ExecutionContext execution_context;
    protected MicroserviceId microservice_id;
    protected ConnectionId connection_id;
    protected Mock<ITimer> timer;
    protected TaskCompletionSource<bool> ping_occurred;

    void Establish()
    {
        microservice_id = Guid.NewGuid();
        connection_id = ConnectionId.New();

        connect_route = $"/api/clients/{microservice_id}/connect/{connection_id}";
        ping_route = $"/api/clients/{microservice_id}/ping/{connection_id}";

        options = new();
        options_instance = new();
        options.SetupGet(_ => _.Value).Returns(options_instance);
        server = new();
        service_provider = new();
        features = new();
        server.SetupGet(_ => _.Features).Returns(features.Object);
        server_addresses = new();
        features.Setup(_ => _.Get<IServerAddressesFeature>()).Returns(server_addresses.Object);
        server_addresses.SetupGet(_ => _.Addresses).Returns(new[] { "http://localhost:5000" });

        client = new();
        service_provider.Setup(_ => _.GetService(typeof(IClient))).Returns(client.Object);

        task_factory = new();
        timer_factory = new();
        execution_context_manager = new();
        connection_lifecycle = new();

        task_factory.Setup(_ => _.Run(IsAny<Func<Task>>())).Returns((Func<Task> function) =>
        {
            function();
            return Task.CompletedTask;
        });

        task_factory.Setup(_ => _.Delay(IsAny<int>())).Returns(Task.CompletedTask);

        execution_context = new(microservice_id, TenantId.NotSet, CorrelationId.New());
        execution_context_manager.SetupGet(_ => _.Current).Returns(execution_context);

        connection_lifecycle.SetupGet(_ => _.ConnectionId).Returns(connection_id);

        ping_occurred = new();

        timer = new();
        timer_factory.Setup(_ =>
            _.Create(
                IsAny<TimerCallback>(),
                IsAny<int>(),
                IsAny<int>(),
                IsAny<object>())).Returns((TimerCallback callback, int _, int __, object? state) =>
                {
                    callback(state);
                    ping_occurred.SetResult(true);
                    return timer.Object;
                });

        connection = new(
            options.Object,
            server.Object,
            service_provider.Object,
            task_factory.Object,
            timer_factory.Object,
            execution_context_manager.Object,
            connection_lifecycle.Object,
            new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase },
            Mock.Of<ILogger<RestKernelConnection>>());
    }
}
