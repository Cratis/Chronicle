// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_CompensationSchemaProcessor;

public class when_processing_event_type_with_compensation_attribute : given.a_json_schema_generator_for<CompensatingEvent>
{
    [Fact] void should_have_compensation_for_key_in_extension_data() => _schema.IsCompensation().ShouldBeTrue();
    [Fact] void should_have_correct_compensated_event_type_id() => _schema.GetCompensationFor().ShouldEqual(nameof(OriginalEvent));
}
