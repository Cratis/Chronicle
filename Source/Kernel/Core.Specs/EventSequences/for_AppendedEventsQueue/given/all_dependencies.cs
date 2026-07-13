// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Tasks;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.given;

public class all_dependencies : Specification
{
    protected ITaskFactory _taskFactory;
    protected IGrainFactory _grainFactory;

    void Establish()
    {
        _taskFactory = Substitute.For<ITaskFactory>();
        _grainFactory = Substitute.For<IGrainFactory>();

        // The real ITaskFactory.Run offloads the queue handler to a background thread. Running it inline
        // on the caller here deadlocks the spec under constrained parallelism (e.g. CI's 2-core agents):
        // the handler's continuations and the test's AwaitQueueDepletion timers contend for the same
        // starved thread pool and never make progress. Offload to a dedicated thread to mirror production.
        _taskFactory
            .When(_ => _.Run(Arg.Any<Func<Task>>()))
            .Do(callInfo => Task.Factory.StartNew(
                callInfo.Arg<Func<Task>>(),
                CancellationToken.None,
                TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default));
    }
}
