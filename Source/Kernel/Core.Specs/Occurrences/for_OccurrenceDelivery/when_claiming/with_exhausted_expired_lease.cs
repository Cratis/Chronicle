// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceDelivery.when_claiming;

public class with_exhausted_expired_lease : given.a_queue
{
    OccurrenceId _id;
    IEnumerable<Occurrence> _result;
    async Task Establish()
    {
        _queue = new(_storage, _definitions, _trigger, new() { MaxAttempts = 1 }, _clock);
        _id = new((await _queue.Record("expiry", "{\"value\":1}", OccurrenceKind.Application)).Id);
        await _queue.Claim("lost", 1, 1);
        _clock.Now = _now.AddSeconds(1);
    }

    async Task Because() => _result = await _queue.Claim("next", 0, 1);
    [Fact] void should_not_start_an_extra_attempt() => _result.ShouldBeEmpty();
    [Fact] async Task should_quarantine_the_exhausted_occurrence() => (await _storage.Get(_id))!.State.ShouldEqual(OccurrenceState.Quarantined);
}
