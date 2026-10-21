// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceDelivery.when_failing;

public class and_the_attempt_limit_is_reached : given.a_queue
{
    Occurrence _claim;
    Occurrence _stored;
    async Task Establish()
    {
        _queue = new(_storage, _definitions, _trigger, new() { MaxAttempts = 1 }, _clock);
        await _queue.Record("quarantine", "{\"value\":1}", OccurrenceKind.Application);
        _claim = (await _queue.Claim("worker", 0, 1)).Single();
    }

    async Task Because()
    {
        await _queue.Fail(new(_claim.Id, _claim.Epoch), "failure");
        _stored = (await _storage.Get(_claim.Id))!;
    }

    [Fact] void should_quarantine_only_the_failed_occurrence() => _stored.State.ShouldEqual(OccurrenceState.Quarantined);
    [Fact] void should_preserve_attempt_count() => _stored.Attempts.ShouldEqual(1);
    [Fact] void should_preserve_the_error() => _stored.LastError.ShouldEqual("failure");
    [Fact] async Task should_not_deliver_it_automatically() => (await _queue.Claim("worker", 0, 1)).ShouldBeEmpty();
}
