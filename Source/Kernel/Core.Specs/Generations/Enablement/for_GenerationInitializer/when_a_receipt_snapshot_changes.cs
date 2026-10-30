// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Generations.Enablement.for_GenerationInitializer;

public class when_a_receipt_snapshot_changes : given.an_initializer
{
    async Task Establish()
    {
        _catalog.Conflict = new("projection-b");
        await _initializer.Initialize(_name);
        _catalog.Conflict = null;
        _snapshot[0] = _snapshot[0] with { Container = new("changed") };
    }
    async Task Because() => _result = await _initializer.Initialize(_name);
    [Fact] void should_refuse() => _result.ShouldBeOfExactType<GenerationInitializationBlocked>();
    [Fact] void should_not_bind() => _catalog.Epochs.ShouldBeEmpty();
}
