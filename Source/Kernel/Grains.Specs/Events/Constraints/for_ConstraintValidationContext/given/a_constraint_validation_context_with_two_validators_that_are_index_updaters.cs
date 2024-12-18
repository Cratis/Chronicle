// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.Events.Constraints.for_ConstraintValidationContext.given;

public class a_constraint_validation_context_with_two_validators_that_are_index_updaters : two_validators_that_are_index_updaters
{
    protected ConstraintValidationContext _context;

    void Establish() => _context = new([_firstValidator, _secondValidator], _eventSourceId, _eventType.Id, _content);
}
