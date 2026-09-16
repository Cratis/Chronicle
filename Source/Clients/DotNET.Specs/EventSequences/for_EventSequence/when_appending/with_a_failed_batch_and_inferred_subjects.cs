// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class with_a_failed_batch_and_inferred_subjects : given.an_acknowledged_append
{
    void Establish()
    {
        _batchResponse.Errors = ["rejected"];
        _batchResponse.Receipts = [];
        _batchResponse.SequenceNumbers = [];
        _eventTypes.HasFor(typeof(EventWithSubject)).Returns(true);
        _eventTypes.GetEventTypeFor(typeof(EventWithSubject)).Returns(new EventType("event", EventTypeGeneration.First));
    }

    async Task Because() => await _eventSequence.AppendMany([new(_source, new EventWithSubject("first-subject")), new(_source, new EventWithSubject("second-subject"))]);

    [Fact] void should_notify_each_attempt() => _notifications.Length.ShouldEqual(2);
    [Fact] void should_preserve_the_subjects_sent_in_the_request() => _notifications.Select(_ => _.Event.Context.Subject.Value).ShouldEqual(_batchRequest.Events.Select(_ => _.Subject));
    [Fact] void should_resolve_subjects_from_each_event() => _notifications.Select(_ => _.Event.Context.Subject.Value).ShouldEqual(["first-subject", "second-subject"]);
    [Fact] void should_not_fabricate_receipts() => _notifications.All(_ => _.Result.Receipt is null).ShouldBeTrue();

    record EventWithSubject([Subject] string PersonId);
}
