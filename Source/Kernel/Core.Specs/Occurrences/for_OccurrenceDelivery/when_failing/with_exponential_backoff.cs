// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceDelivery.when_failing;

public class with_exponential_backoff : given.a_queue
{
    OccurrenceId _id;
    Occurrence _stored;
    async Task Establish()
    {
        _id = new((await _queue.Record("backoff", "{\"value\":1}", OccurrenceKind.Application)).Id);
        var first = (await _queue.Claim("worker", 0, 1)).Single();
        await _queue.Fail(new(first.Id, first.Epoch), "first");
        _clock.Now = _now.AddSeconds(1);
    }

    async Task Because()
    {
        var second = (await _queue.Claim("worker", 0, 1)).Single();
        await _queue.Fail(new(second.Id, second.Epoch), "second");
        _stored = (await _storage.Get(_id))!;
    }

    [Fact] void should_double_the_second_delay() => _stored.NextAttempt.ShouldEqual(_now.AddSeconds(3));
    [Fact] void should_count_claims_not_failure_calls() => _stored.Attempts.ShouldEqual(2);
    [Fact] async Task should_not_claim_before_retry_is_due() => (await _queue.Claim("worker", 0, 1)).ShouldBeEmpty();
}
