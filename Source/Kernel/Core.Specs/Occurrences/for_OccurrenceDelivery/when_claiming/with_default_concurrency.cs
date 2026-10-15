// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences.for_OccurrenceDelivery.when_claiming;

public class with_default_concurrency : given.a_queue
{
    IEnumerable<Occurrence> _claims;
    async Task Establish()
    {
        await _queue.Record("one", "{\"value\":1}", OccurrenceKind.Application);
        await _queue.Record("two", "{\"value\":2}", OccurrenceKind.Application);
    }

    async Task Because() => _claims = await _queue.Claim("worker", 0, 10);
    [Fact] void should_lease_only_one_occurrence() => _claims.Count().ShouldEqual(1);
    [Fact] async Task should_refuse_another_live_lease() => (await _queue.Claim("another", 0, 10)).ShouldBeEmpty();
}
