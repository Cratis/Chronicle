// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_recording_applied_event_markers;

public class and_a_root_join_is_converted : given.a_changeset_converter_recording_markers
{
    UpdateDefinitionAndArrayFilters _result;
    string _slot;

    async Task Because()
    {
        _slot = $"__applied.j.{AppliedEventMarkers.DefinitionId(EventTypeId, "joinId")}";
        _result = await Convert([JoinOn("joinId")], Context());
    }

    [Fact] void should_run_the_join_once() => _joinWrites.ShouldEqual(1);
    [Fact] void should_only_join_documents_that_do_not_record_the_event() =>
        _joinFilter.ShouldEqual(BsonDocument.Parse($$"""{ "joinId": "joined-source", "$nor": [ { "{{_slot}}.s": "{{Stream}}", "{{_slot}}.n": { "$gte": {"$numberLong": "42"} } } ] }"""));
    [Fact] void should_record_the_event_in_the_slot_of_the_join() =>
        _joinUpdate["$set"][_slot].ShouldEqual(new BsonDocument { { "s", Stream }, { "n", new BsonInt64(42) } });
    [Fact] void should_still_advance_the_watermark() => _joinUpdate["$max"].AsBsonDocument.ElementCount.ShouldEqual(1);
    [Fact] void should_not_report_changes_for_the_root_document() => _result.hasChanges.ShouldBeFalse();
}
