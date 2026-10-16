// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceDelivery.when_claiming;

public class and_the_reply_is_lost : given.a_queue
{
    Occurrence _first;
    Occurrence _reclaimed;
    OccurrenceLeaseResult _stale;

    async Task Establish()
    {
        await _queue.Record("claim", "{\"value\":1}", OccurrenceKind.Application);
        _first = (await _queue.Claim("lost-reply", 1, 1)).Single();
        _clock.Now = _now.AddSeconds(1);
    }

    async Task Because()
    {
        _reclaimed = (await _queue.Claim("replacement", 0, 1)).Single();
        _stale = await _queue.Complete(new(_first.Id, _first.Epoch));
    }

    [Fact] void should_raise_the_epoch() => _reclaimed.Epoch.ShouldEqual(2);
    [Fact] void should_count_the_expired_attempt() => _reclaimed.Attempts.ShouldEqual(2);
    [Fact] void should_refuse_the_superseded_token() => _stale.ShouldEqual(OccurrenceLeaseResult.OccurrenceLeaseLost);
}
