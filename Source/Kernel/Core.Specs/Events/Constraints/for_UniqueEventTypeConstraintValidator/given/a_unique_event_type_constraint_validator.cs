// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueEventTypeConstraintValidator.given;

public abstract class a_unique_event_type_constraint_validator : Specification
{
    protected UniqueEventTypeConstraintValidator _validator;
    protected IUniqueEventTypesConstraintsStorage _storage;

    void Establish()
    {
        _storage = Substitute.For<IUniqueEventTypesConstraintsStorage>();
        _validator = new UniqueEventTypeConstraintValidator(Definition, _storage);
    }

    protected abstract UniqueEventTypeConstraintDefinition Definition { get; }
}
