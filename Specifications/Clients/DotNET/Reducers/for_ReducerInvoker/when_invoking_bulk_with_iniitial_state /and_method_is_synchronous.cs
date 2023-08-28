// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Auditing;
using Aksio.Cratis.Identities;

namespace Aksio.Cratis.Reducers.for_ReducerInvoker.when_invoking_bulk_with_initial_state;

public class and_method_is_synchronous : given.a_reducer_invoker_for<SyncReducer>
{
    IEnumerable<EventAndContext> events_and_contexts;
    ReadModel initial;
    InternalReduceResult reduce_result;
    ReadModel result;

    void Establish()
    {
        initial = new(42);
        events_and_contexts = new EventAndContext[]
        {
            new(new ValidEvent(), new(Guid.Empty, 0, DateTimeOffset.Now, DateTimeOffset.Now, TenantId.Development, CorrelationId.New(), Enumerable.Empty<Causation>(), Identity.System)),
            new(new ValidEvent(), new(Guid.Empty, 1, DateTimeOffset.Now, DateTimeOffset.Now, TenantId.Development, CorrelationId.New(), Enumerable.Empty<Causation>(), Identity.System)),
            new(new ValidEvent(), new(Guid.Empty, 2, DateTimeOffset.Now, DateTimeOffset.Now, TenantId.Development, CorrelationId.New(), Enumerable.Empty<Causation>(), Identity.System)),
            new(new ValidEvent(), new(Guid.Empty, 3, DateTimeOffset.Now, DateTimeOffset.Now, TenantId.Development, CorrelationId.New(), Enumerable.Empty<Causation>(), Identity.System))
        };
    }

    async Task Because()
    {
        reduce_result = (await invoker.Invoke(events_and_contexts, initial))!;
        result = reduce_result.State as ReadModel;
    }


    [Fact] void should_only_create_one_instance_of_the_reducer() => service_provider.Verify(_ => _.GetService(typeof(SyncReducer)), Once);
    [Fact] void should_pass_the_events_and_contexts() => reducer.ReceivedEventsAndContexts.ShouldEqual(events_and_contexts);
    [Fact] void should_return_a_read_model_that_has_been_iterated_on() => result.Count.ShouldEqual(events_and_contexts.Count() + initial.Count);
}
