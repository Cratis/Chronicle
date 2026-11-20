// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Kernel.Integration.for_DormantObserver.given;

[DependencyInjection.IgnoreConvention]
public class DormantAlphaReactor : IReactor
{
    public ConcurrentQueue<EventSequenceNumber> Handled { get; } = new();

    public Task OnDormantAlphaEvent(DormantAlphaEvent evt, EventContext context)
    {
        Handled.Enqueue(context.SequenceNumber);
        return Task.CompletedTask;
    }
}
