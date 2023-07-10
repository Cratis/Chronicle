// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Orleans.Runtime;

namespace Aksio.Cratis.Kernel.Grains.Workers.given;

public class a_worker : GrainSpecification
{
    protected Mock<IExecutionContextManager> execution_context_manager;
    protected MyWorkerResult result;
    protected MyWorker worker;
    protected Exception? exception_to_throw;

    protected override Guid GrainId => Guid.NewGuid();

    protected override string GrainKeyExtension => string.Empty;

    protected ExecutionContext? execution_context;

    protected override Grain GetGrainInstance()
    {
        execution_context_manager = new();
        execution_context_manager.Setup(_ => _.Set(IsAny<ExecutionContext>())).Callback<ExecutionContext>(context => execution_context = context);
        execution_context_manager.SetupGet(_ => _.Current).Returns(execution_context!);
        result = new("Hello world");
        worker = new(result, exception_to_throw, execution_context_manager.Object, Mock.Of<ILogger>());
        return worker;
    }

    void Establish()
    {
        timer_registry
            .Setup(_ => _.RegisterTimer(IsAny<IGrainContext>(), IsAny<Func<object, Task>>(), IsAny<object>(), IsAny<TimeSpan>(), IsAny<TimeSpan>()))
            .Returns((Grain __, Func<object, Task> callback, object state, TimeSpan ___, TimeSpan ____) =>
            {
                callback(state);
                return Task.CompletedTask;
            });
    }
}
