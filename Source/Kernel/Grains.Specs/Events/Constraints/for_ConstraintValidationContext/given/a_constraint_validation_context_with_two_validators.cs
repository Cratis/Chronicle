// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Grains.Events.Constraints.for_ConstraintValidationContext.given;

public class a_constraint_validation_context_with_two_validators : Specification
{
    protected IConstraintValidator _firstValidator;
    protected IConstraintValidator _secondValidator;
    protected ConstraintValidationContext _context;
    protected EventSourceId _eventSourceId;
    protected EventType _eventType;
    protected ExpandoObject _content;

    void Establish()
    {
        _firstValidator = Substitute.For<IConstraintValidator>();
        _secondValidator = Substitute.For<IConstraintValidator>();

        _eventSourceId = EventSourceId.New();
        _eventType = new("SomeEvent", 1);
        _content = new();

        _context = new([_firstValidator, _secondValidator], _eventSourceId, _eventType, _content);
    }
}
