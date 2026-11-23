// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation.Webhooks;

namespace Cratis.Chronicle.EventTypes.for_EventTypes.when_retrying_registration;

public class and_storage_previously_failed : given.an_event_types_with_multiple_stores
{
    readonly Exception _failure = new InvalidOperationException("Storage unavailable");
    Exception _caught;

    void Establish() => _register = (_, _) => Task.FromException<bool>(_failure);

    async Task Because()
    {
        _caught = await Catch.Exception(() => _eventTypes.DiscoverAndRegister(_storeA));
        _register = (_, _) => Task.FromResult(true);
        await _eventTypes.DiscoverAndRegister(_storeA);
    }

    [Fact] void should_propagate_the_storage_failure() => _caught.ShouldEqual(_failure);
    [Fact] void should_retain_preparation_for_retry() => _preparations[typeof(WebhookAdded)].ShouldEqual(1);
    [Fact] async Task should_attempt_storage_registration_again() => await ShouldRegister(_storeA, typeof(WebhookAdded), 2);
    [Fact] void should_not_reuse_the_failed_registration_schema() => ReferenceEquals(_schemas[_storeA].First(), _schemas[_storeA].Last()).ShouldBeFalse();
    [Fact] void should_keep_clr_resolution_available() => _eventTypes.GetClrTypeFor(typeof(WebhookAdded).GetEventType().Id).ShouldEqual(typeof(WebhookAdded));
}
