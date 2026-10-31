// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceDelivery.when_claiming;

public class and_an_earlier_occurrence_is_retrying : given.a_queue
{
    OccurrenceId _first;
    Occurrence _later;
    OccurrenceLeaseResult _completed;

    async Task Establish()
    {
        _first = new((await _queue.Record("first", "{\"value\":1}", OccurrenceKind.Application)).Id);
        var first = (await _queue.Claim("worker", 0, 20)).Single();
        await _queue.Fail(new(first.Id, first.Epoch), "retry");
        _clock.Now = _now.AddMilliseconds(1);
        await _queue.Record("later", "{\"value\":2}", OccurrenceKind.Shutdown);
    }

    async Task Because()
    {
        _later = (await _queue.Claim("worker", 0, 20)).Single();
        _completed = await _queue.Complete(new(_later.Id, _later.Epoch));
    }

    [Fact] void should_claim_the_later_occurrence() => _later.Id.ShouldNotEqual(_first);
    [Fact] void should_complete_it_first() => _completed.ShouldEqual(OccurrenceLeaseResult.Updated);
    [Fact] async Task should_leave_the_earlier_attempt_in_backoff() => (await _storage.Get(_first))!.NextAttempt.ShouldEqual(_now.AddSeconds(1));
    [Fact] void should_default_the_lease_to_sixty_seconds() => _later.LeaseExpiry.ShouldEqual(_clock.Now.AddSeconds(60));
}
