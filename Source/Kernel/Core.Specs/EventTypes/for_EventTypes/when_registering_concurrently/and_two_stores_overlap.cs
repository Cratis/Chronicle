// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks;

namespace Cratis.Chronicle.EventTypes.for_EventTypes.when_registering_concurrently;

public class and_two_stores_overlap : given.an_event_types_with_multiple_stores
{
    readonly TaskCompletionSource _enteredA = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _enteredB = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _preparedAfterOverlap;

    void Establish() => _register = async (store, _) =>
    {
        (store == _storeA ? _enteredA : _enteredB).TrySetResult();
        await _release.Task.WaitAsync(TimeSpan.FromSeconds(10));
        return true;
    };

    async Task Because()
    {
        var first = _eventTypes.DiscoverAndRegister(_storeA);
        await _enteredA.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var second = _eventTypes.DiscoverAndRegister(_storeB);
        await _enteredB.Task.WaitAsync(TimeSpan.FromSeconds(10));
        _release.SetResult();
        await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(10));
        _preparedAfterOverlap = _preparations[typeof(WebhookAdded)];
        await _eventTypes.DiscoverAndRegister(_storeA);
    }

    [Fact] async Task should_register_both_attempts_in_the_first_store() => await ShouldRegister(_storeA, typeof(WebhookAdded), 2);
    [Fact] async Task should_complete_registration_in_the_second_store() => await ShouldRegister(_storeB, typeof(WebhookAdded));
    [Fact] void should_reuse_preparation_after_the_overlap() => _preparations[typeof(WebhookAdded)].ShouldEqual(_preparedAfterOverlap);
    [Fact] void should_keep_registration_schemas_independent() => _schemas.Values.SelectMany(schemas => schemas).Distinct().Count().ShouldEqual(3);
    [Fact] void should_keep_equal_json_in_both_stores() => _schemas[_storeA].First().ToJson().ShouldEqual(_schemas[_storeB].Single().ToJson());
}
