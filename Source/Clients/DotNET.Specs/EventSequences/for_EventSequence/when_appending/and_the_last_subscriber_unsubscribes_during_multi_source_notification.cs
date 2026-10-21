// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class and_the_last_subscriber_unsubscribes_during_multi_source_notification : given.an_event_sequence_with_append_notifications
{
    bool _unsubscribed;

    void Establish()
    {
        _subscription = _eventSequence.AppendOperations.Subscribe(_ => { });
        _eventTypes.GetEventTypeFor(typeof(object)).Returns(_ =>
        {
            if (_responseReturned)
            {
                _subscription.Dispose();
                _unsubscribed = true;
            }
            return _eventType;
        });
    }

    async Task Because() => _error = await Catch.Exception(async () => _manyResult = await _eventSequence.AppendMany([new EventForEventSourceId(_eventSourceId, _event)]));

    [Fact] void should_unsubscribe_after_the_response() => _unsubscribed.ShouldBeTrue();
    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_return_success() => _manyResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_preserve_the_durable_position() => _manyResult.SequenceNumbers.Single().Value.ShouldEqual(42UL);
}
