// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Grains.Events.Constraints.for_ConstraintValidatorExtensions.when_creating_violation_from_validator;

public class of_type_unique_constraint_validator : given.a_constraint_validation_context
{
    const string ConstraintName = "SomeUniqueConstraint";

    IConstraintValidator _validator;

    void Establish() => _validator = new UniqueConstraintValidator(new UniqueConstraintDefinition(ConstraintName, []), Substitute.For<IUniqueConstraintsStorage>());

    void Because() => _result = _validator.CreateViolation(_context, _sequenceNumber, _message, _details);

    [Fact] void should_return_a_violation() => _result.ShouldNotBeNull();
    [Fact] void should_have_the_correct_event_type() => _result.EventTypeId.ShouldEqual(_context.EventTypeId);
    [Fact] void should_have_the_correct_sequence_number() => _result.SequenceNumber.ShouldEqual(_sequenceNumber);
    [Fact] void should_have_the_correct_constraint_type() => _result.ConstraintType.ShouldEqual(ConstraintType.Unique);
    [Fact] void should_have_the_correct_constraint_name() => _result.ConstraintName.Value.ShouldEqual(ConstraintName);
    [Fact] void should_have_the_correct_message() => _result.Message.ShouldEqual(_message);
    [Fact] void should_have_the_correct_details() => _result.Details.ShouldEqual(_details);
}
