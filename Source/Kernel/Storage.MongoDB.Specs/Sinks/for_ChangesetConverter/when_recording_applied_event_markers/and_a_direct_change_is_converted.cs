// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_recording_applied_event_markers;

public class and_a_direct_change_is_converted : given.a_changeset_converter_recording_markers
{
    UpdateDefinitionAndArrayFilters _result;
    UpdateDefinitionAndArrayFilters _withoutMarkers;

    async Task Because()
    {
        _result = await Convert([Direct()], Context());
        _withoutMarkers = await Convert([Direct()], null);
    }

    [Fact] void should_record_the_event_in_the_slot_of_the_single_pass() =>
        RenderUpdate(_result.UpdateDefinition)["$set"]["__applied.f.0"].ShouldEqual(new BsonDocument { { "s", Stream }, { "n", new BsonInt64(42) } });
    [Fact] void should_apply_only_when_the_slot_does_not_hold_the_event_or_a_later_one() =>
        Render(_result.AppliedCondition!).ShouldEqual(BsonDocument.Parse($$"""{ "$nor": [ { "__applied.f.0.s": "{{Stream}}", "__applied.f.0.n": { "$gte": {"$numberLong": "42"} } } ] }"""));
    [Fact] void should_not_record_a_slot_without_markers() => RenderUpdate(_withoutMarkers.UpdateDefinition)["$set"].AsBsonDocument.Contains("__applied").ShouldBeFalse();
    [Fact] void should_not_condition_the_write_without_markers() => _withoutMarkers.AppliedCondition.ShouldBeNull();
}
