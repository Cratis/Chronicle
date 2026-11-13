// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_recording_applied_event_markers;

public class and_a_join_is_converted_without_markers : given.a_changeset_converter_recording_markers
{
    async Task Because() => await Convert([JoinOn("joinId")], null);

    [Fact] void should_filter_by_the_join_key_alone() => _joinFilter.ShouldEqual(new BsonDocument("joinId", "joined-source"));
    [Fact] void should_not_record_a_slot() => _joinUpdate["$set"].AsBsonDocument.Contains("__applied").ShouldBeFalse();
}
