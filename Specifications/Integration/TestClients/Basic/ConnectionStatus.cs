// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Cratis.Connections;
using Cratis.Execution;
using Cratis.EventSequences;
using Microsoft.Extensions.DependencyInjection;

namespace Basic;

public class ConnectionStatus : IParticipateInConnectionLifecycle
{
    readonly IExecutionContextManager _executionContextManager;
    readonly IServiceProvider _serviceProvider;

    public ConnectionStatus(
        IExecutionContextManager executionContextManager,
        IServiceProvider serviceProvider)
    {
        _executionContextManager = executionContextManager;
        _serviceProvider = serviceProvider;
    }

    public async Task ClientConnected()
    {
        Console.WriteLine("Cratis Client Connected");
        _executionContextManager.Establish(TenantId.Development, CorrelationId.New(), _executionContextManager.Current.MicroserviceId);
        var eventLog = _serviceProvider.GetRequiredService<IEventLog>();
        await eventLog.Append(Guid.NewGuid().ToString(), new MyEvent());
    }

    public Task ClientDisconnected()
    {
        return Task.CompletedTask;
    }
}
