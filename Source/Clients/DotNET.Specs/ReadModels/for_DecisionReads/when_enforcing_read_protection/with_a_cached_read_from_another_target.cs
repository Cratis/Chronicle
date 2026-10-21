// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.ReadModels.for_DecisionReads.when_enforcing_read_protection;

public class with_a_cached_read_from_another_target : given.a_protected_reader
{
    [Theory]
    [InlineData("other-store", "namespace")]
    [InlineData("store", "other-namespace")]
    public async Task should_refuse_reuse_across_full_targets(string store, string tenant)
    {
        using var frame = _unit.EnforceReadProtection();
        await _reader.GetFor<Model>(_unit, "source");
        var foreign = new DecisionRead<Model>("source", new Model("source"), store, tenant, 5, [new EventType("created", 1)]);
        var error = Catch.Exception(() => _unit.AddDecisionRead(foreign));
        error.ShouldBeOfExactType<ReadDependencyTargetMismatch>();
    }
}
