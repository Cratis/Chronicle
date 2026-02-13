// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Grains.Events.Constraints.for_ConstraintValidatorExtensions.given;

public class a_constraint_validation_context : Specification
{
    protected ConstraintValidationContext _context;
    protected EventSequenceNumber _sequenceNumber;
    protected ConstraintViolationMessage _message;
    protected ConstraintViolationDetails _details;
    protected ConstraintViolation _result;
    protected EventType _eventType;

    void Establish()
    {
        _eventType = new("Some Event", 1);
        _context = new ConstraintValidationContext([], EventSourceId.New(), _eventType.Id, new());
        _sequenceNumber = 42;
        _message = "Some message";
        _details = [];
    }
}
