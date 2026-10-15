// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceDelivery.when_completing;

public class and_the_reply_is_lost : given.a_queue
{
    LeaseToken _token;
    OccurrenceLeaseResult _result;
    async Task Establish()
    {
        await _queue.Record("complete", "{\"value\":1}", OccurrenceKind.Application);
        var claim = (await _queue.Claim("worker", 0, 1)).Single();
        _token = new(claim.Id, claim.Epoch);
        await _queue.Complete(_token);
    }

    async Task Because() => _result = await _queue.Complete(_token);
    [Fact] void should_settle_idempotently() => _result.ShouldEqual(OccurrenceLeaseResult.AlreadyCompleted);
}
