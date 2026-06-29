// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorage.given;

public class a_unique_constraints_storage : Specification
{
    protected const string ConstraintNameValue = "unique-name";
    protected UniqueConstraintsStorage _storage;
    protected UniqueConstraintDefinition _definition;

    void Establish()
    {
        _storage = new();
        _definition = new(ConstraintNameValue, []);
    }
}
