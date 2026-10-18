// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class and_a_single_source_batch_subscriber_throws : given.an_event_sequence_with_append_notifications
{
    bool _notified;

    void Establish() => _subscription = _eventSequence.AppendOperations.Subscribe(_ =>
    {
        _notified = true;
        throw new Exception("Subscriber failed");
    });

    async Task Because() => _error = await Catch.Exception(async () => _manyResult = await _eventSequence.AppendMany(_eventSourceId, [_event]));

    [Fact] void should_notify_the_subscriber() => _notified.ShouldBeTrue();
    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_return_success() => _manyResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_preserve_the_durable_position() => _manyResult.SequenceNumbers.Single().Value.ShouldEqual(42UL);
}
