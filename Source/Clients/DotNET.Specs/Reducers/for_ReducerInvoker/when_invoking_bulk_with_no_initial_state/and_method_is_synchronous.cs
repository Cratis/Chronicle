// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Identities;

namespace Cratis.Chronicle.Reducers.for_ReducerInvoker.when_invoking_bulk_with_no_initial_state;

public class and_method_is_synchronous : given.a_reducer_invoker_for<SyncReducer>
{
    IEnumerable<EventAndContext> events_and_contexts;
    ReduceResult reduce_result;
    ReadModel result;

    void Establish()
    {
        events_and_contexts =
        [
            new(new ValidEvent(), new(Guid.Empty, 0, DateTimeOffset.UtcNow, EventStoreName.NotSet, EventStoreNamespaceName.NotSet, CorrelationId.New(), [], Identity.System)),
            new(new ValidEvent(), new(Guid.Empty, 1, DateTimeOffset.UtcNow, EventStoreName.NotSet, EventStoreNamespaceName.NotSet, CorrelationId.New(), [], Identity.System)),
            new(new ValidEvent(), new(Guid.Empty, 2, DateTimeOffset.UtcNow, EventStoreName.NotSet, EventStoreNamespaceName.NotSet, CorrelationId.New(), [], Identity.System)),
            new(new ValidEvent(), new(Guid.Empty, 3, DateTimeOffset.UtcNow, EventStoreName.NotSet, EventStoreNamespaceName.NotSet, CorrelationId.New(), [], Identity.System))
        ];
    }

    async Task Because()
    {
        reduce_result = (await invoker.Invoke(service_provider.Object, events_and_contexts, null))!;
        result = reduce_result.ModelState as ReadModel;
    }

    [Fact] void should_only_create_one_instance_of_the_reducer() => service_provider.Verify(_ => _.GetService(typeof(SyncReducer)), Once);
    [Fact] void should_pass_the_events_and_contexts() => reducer.ReceivedEventsAndContexts.ShouldEqual(events_and_contexts);
    [Fact] void should_return_a_read_model_that_has_been_iterated_on() => result.Count.ShouldEqual(events_and_contexts.Count());
}
