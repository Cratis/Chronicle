// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_DecimalBulkIdentity;

public class when_prefetching_absent_equivalent_keys : given.a_decimal_bulk_sink
{
    void Establish() => _documents = [];

    async Task Because()
    {
        await _sink.PrefetchPage([_original]);
        _answer = await _sink.FindOrDefault(_equivalent);
    }

    [Fact] void should_share_absence_knowledge() => _answer.ShouldBeNull();
    [Fact] void should_not_issue_a_second_query() => _reads.ShouldEqual(1);
}
