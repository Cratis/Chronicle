// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

public class when_generating_schema_for_type_with_format_types : given.a_json_schema_generator
{
    record TypeWithFormats(Guid AnId, int ACount, DateTimeOffset OccurredAt);

    JsonSchema _result;

    void Because() => _result = _generator.Generate(typeof(TypeWithFormats));

    [Fact] void should_inject_guid_format_for_guid_property() => _result.ActualProperties["AnId"].Format.ShouldEqual("guid");
    [Fact] void should_inject_int32_format_for_int_property() => _result.ActualProperties["ACount"].Format.ShouldEqual("int32");
    [Fact] void should_inject_date_time_offset_format_for_date_time_offset_property() => _result.ActualProperties["OccurredAt"].Format.ShouldEqual("date-time-offset");
}
