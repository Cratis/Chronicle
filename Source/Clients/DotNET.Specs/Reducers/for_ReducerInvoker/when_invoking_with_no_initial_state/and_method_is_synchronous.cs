// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Identities;

namespace Cratis.Chronicle.Reducers.for_ReducerInvoker.when_invoking_with_no_initial_state;

public class and_method_is_synchronous : given.a_reducer_invoker_for<SyncReducer>
{
    ValidEvent @event;
    EventContext event_context;
    ReduceResult result;

    void Establish()
    {
        @event = new();
        event_context = new(Guid.Empty, 0, DateTimeOffset.UtcNow, EventStoreName.NotSet, EventStoreNamespaceName.NotSet, CorrelationId.New(), [], Identity.System);
    }

    async Task Because() => result = await invoker.Invoke(service_provider.Object, [new(@event, event_context)], null);

    [Fact] void should_pass_the_event() => reducer.ReceivedEvents.First().ShouldEqual(@event);
    [Fact] void should_pass_no_read_model() => reducer.ReceivedReadModels.First().ShouldBeNull();
    [Fact] void should_pass_the_event_context() => reducer.ReceivedEventContexts.First().ShouldEqual(event_context);
    [Fact] void should_return_the_read_model() => result.ModelState.ShouldEqual(reducer.ReturnedReadModel);
}
