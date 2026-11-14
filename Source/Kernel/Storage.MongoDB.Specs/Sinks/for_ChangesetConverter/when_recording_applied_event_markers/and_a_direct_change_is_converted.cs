// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

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

    [Fact] void should_write_a_guarded_pipeline() => _result.IsConditional.ShouldBeTrue();
    [Fact] void should_register_the_pipeline_as_marked() => AppliedEventMarkerUpdates.IsMarked(_result.UpdateDefinition).ShouldBeTrue();
    [Fact] void should_record_the_event_in_the_slot_of_the_single_pass() => Json(_result).ShouldContain("\"__applied.f.0\"");
    [Fact] void should_not_need_array_filters() => _result.ArrayFilters.ShouldBeEmpty();
    [Fact] void should_not_need_null_parent_repairs() => _result.NullParentPaths.ShouldBeEmpty();
    [Fact] void should_write_a_classic_update_without_markers() => _withoutMarkers.IsConditional.ShouldBeFalse();
    [Fact] void should_not_record_a_slot_without_markers() => RenderUpdate(_withoutMarkers.UpdateDefinition)["$set"].AsBsonDocument.Contains("__applied").ShouldBeFalse();

    static string Json(UpdateDefinitionAndArrayFilters result) => result.UpdateDefinition.Render(new RenderArgs<BsonDocument>(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry)).ToJson();
}
