// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_DecimalBulkIdentity;

public class when_updating_an_equivalent_key : given.a_decimal_bulk_sink
{
    async Task Establish() => await _sink.PrefetchPage([_original]);

    async Task Because()
    {
        await _sink.ApplyChanges(_equivalent, Changes(), 43UL);
        _sink.ClearPrefetchPage();
        _answer = await _sink.FindOrDefault(_original);
    }

    [Fact] void should_return_the_pending_update() => ((IDictionary<string, object?>)_answer)["count"].ShouldEqual(2);
    [Fact] void should_not_reread_the_document() => _reads.ShouldEqual(1);
}
