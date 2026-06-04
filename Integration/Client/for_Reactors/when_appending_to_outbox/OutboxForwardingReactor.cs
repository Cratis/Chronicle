// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Integration.for_Reactors.when_appending_to_outbox;

[DependencyInjection.IgnoreConvention]
public class OutboxForwardingReactor(TaskCompletionSource tcs, IEventStore eventStore) : IReactor
{
    public async Task On(SomeEvent @event, EventContext context)
    {
        await eventStore.GetEventSequence(EventSequenceId.Outbox)
            .Append(context.EventSourceId, @event);
        tcs.TrySetResult();
    }
}
