// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Metrics;
using Cratis.Tasks;
using Cratis.Traces;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_deactivating.given;

public class a_queue_with_a_gated_worker : Specification
{
    protected readonly TaskCompletionSource _worker = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly CancellationTokenSource _deadline = new();
    protected AppendedEventsQueue _queue;

    void Establish()
    {
        var tasks = Substitute.For<ITaskFactory>();
        tasks.Run(Arg.Any<Func<Task>>()).Returns(_worker.Task);
        var budget = Substitute.For<IShutdownDrainBudget>();
        budget.IsStopping.Returns(true);
        budget.Token.Returns(_deadline.Token);
        _queue = new(
            tasks,
            Substitute.For<IGrainFactory>(),
            Substitute.For<IMeter<AppendedEventsQueue>>(),
            Substitute.For<IActivitySource<AppendedEventsQueue>>(),
            Options.Create(new ChronicleOptions()),
            NullLogger<AppendedEventsQueue>.Instance,
            budget);
    }

    void Destroy()
    {
        _worker.TrySetResult();
        _queue.Dispose();
        _deadline.Dispose();
    }
}
