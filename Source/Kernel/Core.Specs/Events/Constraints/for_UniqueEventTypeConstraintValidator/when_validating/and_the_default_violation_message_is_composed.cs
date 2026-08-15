// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueEventTypeConstraintValidator.when_validating;

/// <summary>
/// The default message is composed server-side and travels back to the caller as a validation result, so whatever it
/// names is a response body rather than a diagnostic. It must not name the event source id: that value is the stream
/// identity and the compliance subject, it cannot be encrypted because correlation depends on reading it, and a
/// caller that supplied no message of its own cannot strip it. It is also the one value the caller already holds.
/// </summary>
/// <remarks>
/// The absence is asserted rather than the exact text, because nothing else in the repository pins this string — a
/// regression that puts the id back would otherwise reach a client unnoticed. The details are checked for the same
/// reason: relocating the id to the structured channel would move it, not withhold it, since the details travel the
/// same route to the same caller.
/// </remarks>
public class and_the_default_violation_message_is_composed : given.a_unique_event_type_constraint_validator_with_valid_definition
{
    ConstraintValidationResult _result;

    void Establish() =>
        _storage
            .IsAllowed(Arg.Any<UniqueEventTypeConstraintDefinition>(), Arg.Any<EventSourceId>(), Arg.Any<string>())
            .Returns((false, (EventSequenceNumber)43U));

    async Task Because() => _result = await _validator.Validate(_context);

    [Fact] void should_not_name_the_event_source_id() => _result.Violations[0].Message.Value.Contains(_context.EventSourceId.Value, StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_carry_the_event_source_id_in_the_details() => _result.Violations[0].Details.Values.Any(_ => _.Contains(_context.EventSourceId.Value, StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_name_the_event_type() => _result.Violations[0].Message.Value.ShouldContain(_context.EventTypeId.Value);
    [Fact] void should_name_the_sequence_number_it_conflicts_with() => _result.Violations[0].Message.Value.ShouldContain("43");
}
