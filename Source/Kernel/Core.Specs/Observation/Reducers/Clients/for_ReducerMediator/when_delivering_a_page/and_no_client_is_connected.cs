// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducerMediator.when_delivering_a_page;

public class and_no_client_is_connected : given.a_page_to_deliver
{
    void Because() => DeliverThePage();

    [Fact] void should_answer_every_partition_as_disconnected() => _completions.Values.All(_ => _.Task.Result.ObserverResult.State == ObserverSubscriberState.Disconnected).ShouldBeTrue();
}
