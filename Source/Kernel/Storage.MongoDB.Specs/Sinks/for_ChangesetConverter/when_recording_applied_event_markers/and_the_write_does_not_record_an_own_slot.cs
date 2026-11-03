// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_recording_applied_event_markers;

public class and_the_write_does_not_record_an_own_slot : given.a_changeset_converter_recording_markers
{
    UpdateDefinitionAndArrayFilters _result;

    async Task Because() => _result = await Convert([Direct()], Context(recordOwnSlot: false));

    [Fact] void should_not_record_a_slot() => RenderUpdate(_result.UpdateDefinition)["$set"].AsBsonDocument.Contains("__applied").ShouldBeFalse();
    [Fact] void should_not_condition_the_write() => _result.AppliedCondition.ShouldBeNull();
}
