// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Catch = Cratis.Monads.Catch;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookMediator.when_calling_on_next;

public class with_an_empty_store_scoped_batch : given.a_store_scoped_delivery
{
    Catch _allowed;
    Catch _rejected;
    Catch _global;

    async Task Because()
    {
        _allowed = await _mediator.OnNext("orders", _target, "partition", []);
        _rejected = await _mediator.OnNext("other", _target, "partition", []);
        _global = await _mediator.OnNext(null, _target, "partition", []);
    }

    [Fact] void should_deliver_for_the_allowed_store_without_events() => _allowed.IsSuccess.ShouldBeTrue();
    [Fact] void should_reject_the_same_target_for_another_store() => _rejected.IsSuccess.ShouldBeFalse();
    [Fact] void should_reject_the_target_globally() => _global.IsSuccess.ShouldBeFalse();
    [Fact] void should_only_contact_the_target_for_the_allowed_store() => _receiver.Stores.ShouldContainOnly((EventStoreName)"orders");
}
