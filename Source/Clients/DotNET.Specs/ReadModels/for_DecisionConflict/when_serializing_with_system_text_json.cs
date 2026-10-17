// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.ReadModels.for_DecisionConflict;

public class when_serializing_with_system_text_json : Specification
{
    static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);

    record BookStock;

    DecisionConflict _conflict;
    JsonElement _result;

    void Establish() => _conflict = new DecisionConflict(typeof(BookStock), "978-0-00-000000-0")
    {
        ConflictingEventTypeId = "BookBorrowed",
        ConflictingEventSequenceNumber = 1042
    };

    void Because() => _result = JsonSerializer.SerializeToElement(_conflict, _options);

    [Fact] void should_not_include_the_clr_type() => _result.TryGetProperty("readModelType", out _).ShouldBeFalse();
    [Fact] void should_include_the_read_model_identifier() => _result.GetProperty("readModel").GetString().ShouldEqual(typeof(BookStock).GetReadModelIdentifier().Value);
    [Fact] void should_include_the_key() => _result.GetProperty("key").GetString().ShouldEqual("978-0-00-000000-0");
    [Fact] void should_include_the_conflicting_event_type() => _result.GetProperty("conflictingEventTypeId").GetString().ShouldEqual("BookBorrowed");
    [Fact] void should_include_the_conflicting_event_sequence_number() => _result.GetProperty("conflictingEventSequenceNumber").GetUInt64().ShouldEqual(1042UL);
}
