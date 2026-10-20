// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Catch = Cratis.Monads.Catch;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookMediator.when_calling_on_next;

public class with_store_scoped_hosts_and_events : given.a_store_scoped_delivery
{
    Catch _result;

    async Task Because() => _result = await _mediator.OnNext(
        "orders",
        _target,
        "partition",
        [AppendedEvent.Empty() with { Context = EventContext.Empty with { EventStore = "untrusted-payload-store" } }]);

    [Fact] void should_deliver_using_the_explicit_store_policy() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_tag_the_request_with_the_explicit_store() => _receiver.Stores.ShouldContainOnly((EventStoreName)"orders");
}
