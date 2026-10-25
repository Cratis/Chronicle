// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Integration.for_Reactors.when_handling_event.and_the_handler_never_returns;

[DependencyInjection.IgnoreConvention]
public class ReactorWithHangingHandler(EventSourceId hangingSource, Task release) : IReactor
{
    public TaskCompletionSource HandlerEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource<EventSourceId> HandledSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public Task HangingHandler = Task.CompletedTask;

    public Task Handle(SomeEvent evt, EventContext context)
    {
        if (context.EventSourceId == hangingSource)
        {
            HangingHandler = release;
            HandlerEntered.TrySetResult();
            return HangingHandler;
        }

        HandledSource.TrySetResult(context.EventSourceId);
        return Task.CompletedTask;
    }
}
