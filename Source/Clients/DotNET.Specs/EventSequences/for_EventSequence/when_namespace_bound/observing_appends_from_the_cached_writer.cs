// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_namespace_bound;

public class observing_appends_from_the_cached_writer : given.a_namespace_bound_append
{
    int _cachedNotifications;
    int _boundNotifications;
    IDisposable _cachedSubscription;
    IDisposable _boundSubscription;

    void Establish()
    {
        _cachedSubscription = _unbound.AppendOperations.Subscribe(_ => _cachedNotifications++);
        _boundSubscription = _eventSequence.AppendOperations.Subscribe(_ => _boundNotifications++);
    }

    async Task Because() => await ((IEventSequenceWithEffectReceipts)_unbound).AppendManyWithEffectReceipt(
        [new("source", "event")], _scopes, _receipt, null);

    [Fact] void should_notify_the_cached_subscriber_added_before_binding() => _notifications.ShouldEqual(1);
    [Fact] void should_notify_the_cached_subscriber_added_after_binding() => _cachedNotifications.ShouldEqual(1);
    [Fact] void should_notify_the_bound_subscriber_added_after_binding() => _boundNotifications.ShouldEqual(1);

    void Destroy()
    {
        _cachedSubscription.Dispose();
        _boundSubscription.Dispose();
    }
}
