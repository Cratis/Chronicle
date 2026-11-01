// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Connections.for_ConnectionLifecycle.given;

public class a_lifecycle_with_ambient_state : Specification
{
    protected readonly AsyncLocal<string> _ambient = new();
    protected ConnectionLifecycle _lifecycle;
    protected Activity _activity;
    protected string _handlerAmbient;
    protected Activity _handlerActivity;
    protected string _continuedAmbient;
    protected Activity _continuedActivity;
    protected string _callerAmbient;
    protected Activity _callerActivity;
    protected bool _callerFlowSuppressed;

    void Establish() => _lifecycle = new ConnectionLifecycle(NullLogger<ConnectionLifecycle>.Instance);

    protected void SetCallerState()
    {
        _ambient.Value = "request-tenant";
        _activity = new Activity("request").Start();
    }

    protected async Task Handle()
    {
        _handlerAmbient = _ambient.Value;
        _handlerActivity = Activity.Current;
        await Task.Yield();
        _continuedAmbient = _ambient.Value;
        _continuedActivity = Activity.Current;
    }

    protected void RecordCallerState()
    {
        _callerAmbient = _ambient.Value;
        _callerActivity = Activity.Current;
        _callerFlowSuppressed = ExecutionContext.IsFlowSuppressed();
    }

    void Destroy() => _activity?.Dispose();
}
