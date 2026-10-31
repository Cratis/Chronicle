// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_DecimalBulkIdentity;

public class when_deleting_an_equivalent_key : given.a_decimal_bulk_sink
{
    async Task Establish() => await _sink.PrefetchPage([_original]);

    async Task Because()
    {
        var removal = Changes();
        removal.Remove();
        await _sink.ApplyChanges(_equivalent, removal, 43UL);
        _answer = await _sink.FindOrDefault(_original);
    }

    [Fact] void should_report_the_pending_delete() => _answer.ShouldBeNull();
    [Fact] void should_not_reread_the_deleted_document() => _reads.ShouldEqual(1);
}
