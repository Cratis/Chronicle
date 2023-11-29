// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Aksio.Cratis.Connections;
using Aksio.Execution;
using Aksio.Cratis.EventSequences;

namespace Basic;

public class ConnectionStatus : IParticipateInConnectionLifecycle
{
    readonly IExecutionContextManager _executionContextManager;
    readonly IEventLog _eventLog;

    public ConnectionStatus(IExecutionContextManager executionContextManager, IEventLog eventLog)
    {
        _executionContextManager = executionContextManager;
        _eventLog = eventLog;
    }

    public async Task ClientConnected()
    {
        Console.WriteLine("Cratis Client Connected");
        _executionContextManager.Establish(TenantId.Development, CorrelationId.New(), MicroserviceId.Unspecified);
        await _eventLog.Append(Guid.NewGuid().ToString(), new MyEvent());
    }

    public Task ClientDisconnected()
    {
        return Task.CompletedTask;
    }
}
