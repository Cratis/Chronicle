// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Kernel.Integration.for_Observer.given;

[DependencyInjection.IgnoreConvention]
public class QuarantineRecoveryReactor : IReactor
{
    TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    volatile RecoveryMode _mode;

    public enum RecoveryMode
    {
        Succeed,
        Fail,
        BlockUntilReleased
    }

    public RecoveryMode Mode
    {
        get => _mode;
        set => _mode = value;
    }

    public TaskCompletionSource Failed { get; private set; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource RetryReached { get; private set; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource RetryHandled { get; private set; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public ConcurrentQueue<EventSequenceNumber> HandledSequenceNumbers { get; } = new();

    public void Reset()
    {
        Mode = RecoveryMode.Succeed;
        Failed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        RetryReached = new(TaskCreationOptions.RunContinuationsAsynchronously);
        RetryHandled = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        HandledSequenceNumbers.Clear();
    }

    public void Release() => _release.TrySetResult();

    public async Task OnQuarantineRecoveryEvent(QuarantineRecoveryEvent evt, EventContext context)
    {
        var mode = Mode;
        if (mode == RecoveryMode.Fail)
        {
            Failed.TrySetResult();
            throw new QuarantineRecoveryReactorFailure();
        }

        if (mode == RecoveryMode.BlockUntilReleased)
        {
            RetryReached.TrySetResult();
            await _release.Task.WaitAsync(TimeSpan.FromSeconds(25));
        }

        HandledSequenceNumbers.Enqueue(context.SequenceNumber);
        if (mode == RecoveryMode.BlockUntilReleased)
        {
            RetryHandled.TrySetResult();
        }
    }
}
