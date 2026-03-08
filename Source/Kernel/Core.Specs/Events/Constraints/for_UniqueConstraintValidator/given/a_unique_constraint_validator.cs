// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintValidator.given;

public abstract class a_unique_constraint_validator : Specification
{
    protected UniqueConstraintValidator _validator;
    protected IUniqueConstraintsStorage _storage;

    void Establish()
    {
        _storage = Substitute.For<IUniqueConstraintsStorage>();
        _validator = new UniqueConstraintValidator(Definition, _storage);
    }

    protected abstract UniqueConstraintDefinition Definition { get; }
}
