// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceQueue.when_rearming;

public class with_an_authenticated_operator : given.a_queue_grain
{
    OccurrenceId _id;
    bool _result;

    async Task Establish()
    {
        _id = new((await _queue.Record("rearm", "{\"value\":1}", OccurrenceKind.Application)).Id);
        var claim = (await _queue.Claim("worker", 0, 1)).Single();
        await _storage.Fail(new(claim.Id, claim.Epoch), "terminal", null);
    }

    async Task Because() => _result = await _grain.Retry(_id.Value, "authenticated-operator");
    [Fact] void should_rearm_the_occurrence() => _result.ShouldBeTrue();
    [Fact] async Task should_record_the_principal_with_the_transition() => (await _storage.Get(_id))!.RetriedBy.ShouldEqual("authenticated-operator");
    [Fact] async Task should_allow_a_fresh_attempt() => (await _queue.Claim("next", 0, 1)).Single().Attempts.ShouldEqual(1);
}
