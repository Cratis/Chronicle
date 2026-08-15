// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintValidator.when_validating;

/// <summary>
/// The sibling of the unique-event-type default, and the same rule: the message is a response body, so a value that
/// is identifying by the very nature of being constrained for uniqueness stays out of it, and so does the event
/// source id. Naming the property is the actionable half — the caller already holds both.
/// </summary>
/// <remarks>
/// Nothing else pins this text either, so the assertions state what must be absent rather than the exact wording.
/// The offending value remains reachable through the violation details, which
/// <c>and_the_violation_details_are_read</c> covers.
/// </remarks>
public class and_the_default_violation_message_is_composed : given.a_unique_constraint_validator_with_valid_definition
{
    const string OffendingValue = "the-offending-value";

    ConstraintValidationResult _result;

    void Establish()
    {
        SetPropertyValue(OffendingValue);
        _storage
            .IsAllowed(Arg.Any<EventSourceId>(), Arg.Any<UniqueConstraintDefinition>(), Arg.Any<UniqueConstraintValue>())
            .Returns((false, (EventSequenceNumber)43U));
    }

    async Task Because() => _result = await _validator.Validate(_context);

    [Fact] void should_not_name_the_offending_value() => _result.Violations[0].Message.Value.Contains(OffendingValue, StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_name_the_event_source_id() => _result.Violations[0].Message.Value.Contains(_context.EventSourceId.Value, StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_name_the_offending_property() => _result.Violations[0].Message.Value.ShouldContain(Property);
    [Fact] void should_name_the_event_type() => _result.Violations[0].Message.Value.ShouldContain(_context.EventTypeId.Value);
    [Fact] void should_name_the_sequence_number_it_conflicts_with() => _result.Violations[0].Message.Value.ShouldContain("43");
}
