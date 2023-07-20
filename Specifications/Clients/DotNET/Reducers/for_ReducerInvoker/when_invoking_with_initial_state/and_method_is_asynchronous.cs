// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Reducers.for_ReducerInvoker.when_invoking_with_initial_state;

public class and_method_is_asynchronous : given.a_reducer_invoker_for<AsyncReducer>
{
    ValidEvent @event;
    EventContext event_context;
    ReadModel current;
    object result;

    void Establish()
    {
        @event = new();
        current = new();
        event_context = new(Guid.Empty, 0, DateTimeOffset.Now, DateTimeOffset.Now, TenantId.Development, CorrelationId.New(), CausationId.System, CausedBy.System);
    }

    async Task Because() => result = await invoker.Invoke(new EventAndContext[] { new(@event, event_context) }, current);

    [Fact] void should_pass_the_event() => reducer.ReceivedEvents.First().ShouldEqual(@event);
    [Fact] void should_pass_no_read_model() => reducer.ReceivedReadModels.First().ShouldBeNull();
    [Fact] void should_pass_the_event_context() => reducer.ReceivedEventContexts.First().ShouldEqual(event_context);
    [Fact] void should_return_the_read_model() => result.ShouldEqual(reducer.ReturnedReadModel);
}
