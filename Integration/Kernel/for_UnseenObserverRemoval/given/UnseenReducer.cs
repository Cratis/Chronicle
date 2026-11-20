// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.given;

[DependencyInjection.IgnoreConvention]
public class UnseenReducer : IReducerFor<UnseenReadModel>, IUnseenObserver
{
    public ConcurrentQueue<EventSequenceNumber> Handled { get; } = new();

    public Task<UnseenReadModel?> OnUnseenEvent(UnseenEvent evt, UnseenReadModel? input, EventContext context)
    {
        Handled.Enqueue(context.SequenceNumber);
        return Task.FromResult<UnseenReadModel?>(new((input?.Count ?? 0) + 1, evt.Name));
    }
}
