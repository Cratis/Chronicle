// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

public class when_generating_schema_for_record_with_struct_default_values : given.a_json_schema_generator
{
    record TypeWithStructDefaults(DateTimeOffset OccurredAt = default, Guid Id = default);

    Exception? _error;
    JsonSchema _result = null!;

    void Because() => _error = Catch.Exception(() => _result = _generator.Generate(typeof(TypeWithStructDefaults)));

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_generate_a_schema() => _result.ShouldNotBeNull();
    [Fact] void should_have_occurred_at_property() => _result.ActualProperties.ContainsKey("OccurredAt").ShouldBeTrue();
    [Fact] void should_have_id_property() => _result.ActualProperties.ContainsKey("Id").ShouldBeTrue();
    [Fact] void should_inject_date_time_offset_format_for_occurred_at() => _result.ActualProperties["OccurredAt"].Format.ShouldEqual("date-time-offset");
    [Fact] void should_inject_guid_format_for_id() => _result.ActualProperties["Id"].Format.ShouldEqual("guid");
}
