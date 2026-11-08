// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.InMemory.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.StoreKeys.for_UniqueConstraintEncodingStorage.given;

public class a_key_storage : Specification
{
    protected IUniqueConstraintEncodingStorage _storage;

    void Establish() => _storage = new UniqueConstraintEncodingStorage();
}
