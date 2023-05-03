// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.Workers;

public record MyWorkerRequest(string Message);
public record MyWorkerResult(string Message);

public class MyWorker : Worker<MyWorkerRequest, MyWorkerResult>
{
    readonly MyWorkerResult _result;
    readonly Exception _exceptionToThrow;
    readonly IExecutionContextManager _executionContextManager;

    public MyWorkerRequest RequestHandedToPerformWork { get; private set; } = null!;
    public ExecutionContext ExecutionContext { get; private set; } = null!;

    public MyWorker(
        MyWorkerResult result,
        Exception? exceptionToThrow,
        IExecutionContextManager executionContextManager,
        ILogger logger) : base(executionContextManager, logger)
    {
        _result = result;
        _exceptionToThrow = exceptionToThrow;
        _executionContextManager = executionContextManager;
    }

    protected override Task<MyWorkerResult> PerformWork(MyWorkerRequest request)
    {
        ExecutionContext = _executionContextManager.Current;
        RequestHandedToPerformWork = request;
        if (_exceptionToThrow != null) throw _exceptionToThrow;
        return Task.FromResult(_result);
    }
}
