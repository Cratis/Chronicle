// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_finding_keys_holding_a_child;

public abstract class and_no_parent_holds_it<THarness> : given.a_sink_with_parents_holding_children<THarness>
    where THarness : ISinkHarness, new()
{
    async Task Establish() => await SeedParents();

    async Task Because() => _keys = await _sink.FindKeysHoldingChild(Removal("nobody-has-this"));

    [Fact] public void should_find_no_parent() => _keys.ShouldBeEmpty();
}
