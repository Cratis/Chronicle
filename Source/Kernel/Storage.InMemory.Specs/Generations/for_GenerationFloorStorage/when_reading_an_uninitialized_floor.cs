// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Storage.InMemory.Generations.for_GenerationFloorStorage;

public class when_reading_an_uninitialized_floor : given.a_floor_storage
{
    GenerationFloor? _result;
    IReadOnlyList<GenerationFloor> _history;
    async Task Because()
    {
        _result = await _storage.Get();
        _history = await _storage.GetHistory();
    }
    [Fact] void should_return_no_floor() => _result.ShouldBeNull();
    [Fact] void should_leave_no_audit_rows() => _history.ShouldBeEmpty();
}
