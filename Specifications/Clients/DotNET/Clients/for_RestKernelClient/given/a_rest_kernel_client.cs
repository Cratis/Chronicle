// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Json;
using Aksio.Cratis.Commands;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Tasks;
using Aksio.Cratis.Timers;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Clients.for_RestKernelClient.given;


public class a_rest_kernel_client : Specification
{
    protected static HttpResponseMessage success_message
    {
        get
        {
            var message = new HttpResponseMessage(HttpStatusCode.OK);
            message.Content = JsonContent.Create(CommandResult.Success);
            return message;
        }
    }

    protected static HttpResponseMessage not_found_message => new(HttpStatusCode.NotFound);
    protected static string connect_route;
    protected static string ping_route;
    protected Mock<ITaskFactory> task_factory;
    protected Mock<ITimerFactory> timer_factory;
    protected Mock<IExecutionContextManager> execution_context_manager;
    protected Mock<IClientLifecycle> client_lifecycle;
    protected KernelClient client;
    protected ExecutionContext execution_context;

    protected MicroserviceId microservice_id;
    protected ConnectionId connection_id;
    protected Mock<ITimer> timer;

    void Establish()
    {
        microservice_id = Guid.NewGuid();
        connection_id = ConnectionId.New();

        connect_route = $"/api/clients/{microservice_id}/connect/{connection_id}";
        ping_route = $"/api/clients/{microservice_id}/ping/{connection_id}";

        task_factory = new();
        timer_factory = new();
        execution_context_manager = new();
        client_lifecycle = new();

        task_factory.Setup(_ => _.Run(IsAny<Func<Task>>())).Returns((Func<Task> function) =>
        {
            function();
            return Task.CompletedTask;
        });

        task_factory.Setup(_ => _.Delay(IsAny<int>())).Returns(Task.CompletedTask);

        execution_context = new(microservice_id, TenantId.NotSet, CorrelationId.New(), CausationId.System, CausedBy.System);
        execution_context_manager.SetupGet(_ => _.Current).Returns(execution_context);

        client_lifecycle.SetupGet(_ => _.ConnectionId).Returns(connection_id);

        timer = new();
        timer_factory.Setup(_ =>
            _.Create(
                IsAny<TimerCallback>(),
                IsAny<int>(),
                IsAny<int>(),
                IsAny<object>())).Returns((TimerCallback callback, int due, int period, object? state) =>
                {
                    callback(state);
                    return timer.Object;
                });

        client = new(
            task_factory.Object,
            timer_factory.Object,
            execution_context_manager.Object,
            new Uri("https://www.somewhere.com"),
            client_lifecycle.Object,
            new(),
            Mock.Of<ILogger<RestKernelClient>>());
    }
}
