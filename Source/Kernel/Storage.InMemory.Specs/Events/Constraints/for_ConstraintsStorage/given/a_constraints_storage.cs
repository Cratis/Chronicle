// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_ConstraintsStorage.given;

public class a_constraints_storage : Specification
{
    protected ConstraintsStorage _storage;

    void Establish() => _storage = new();
}
