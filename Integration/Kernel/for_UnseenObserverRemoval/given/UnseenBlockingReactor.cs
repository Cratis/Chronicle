// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.given;

[DependencyInjection.IgnoreConvention]
public class UnseenBlockingReactor : IReactor, IUnseenObserver
{
    TaskCompletionSource? _gate;

    public ConcurrentQueue<EventSequenceNumber> Handled { get; } = new();

    /// <summary>
    /// Makes the reactor hold every event it is given until the client goes away.
    /// </summary>
    public void Block() => _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public async Task OnUnseenEvent(UnseenEvent evt, EventContext context)
    {
        Handled.Enqueue(context.SequenceNumber);
        if (_gate is { } gate)
        {
            await gate.Task;
        }
    }
}
