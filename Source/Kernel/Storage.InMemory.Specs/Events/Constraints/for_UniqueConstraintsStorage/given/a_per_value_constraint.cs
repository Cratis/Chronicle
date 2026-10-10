// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorage.given;

public class a_per_value_constraint : a_unique_constraints_storage
{
    protected EventSourceId _owner;
    protected EventSourceId _otherOwner;

    async Task Establish()
    {
        _definition = _definition with { Mode = UniqueConstraintMode.PerValue };
        _owner = EventSourceId.New();
        _otherOwner = EventSourceId.New();
        await _storage.Save(_owner, _definition, 42L, "first");
        await _storage.Save(_owner, _definition, 43L, "second");
        await _storage.Save(_owner, _definition, 44L, "scoped", "another-scope");
        await _storage.Save(_otherOwner, _definition, 45L, "foreign");
    }
}
