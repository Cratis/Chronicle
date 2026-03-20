// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_CompensationSchemaProcessor;

public class when_processing_event_type_without_compensation_attribute : given.a_processor_and_a_context_for<EventWithoutCompensation>
{
    void Because() => _processor.Process(_context);

    [Fact] void should_not_have_compensation_for_key_in_extension_data() => _context.Schema.IsCompensation().ShouldBeFalse();
    [Fact] void should_not_have_compensated_event_type_id() => _context.Schema.GetCompensationFor().ShouldBeNull();
}
