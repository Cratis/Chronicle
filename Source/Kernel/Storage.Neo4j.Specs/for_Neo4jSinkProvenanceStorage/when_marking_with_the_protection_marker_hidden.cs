// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;
using static Cratis.Chronicle.Storage.Neo4j.Neo4jSinkProvenanceStatements;

namespace Cratis.Chronicle.Storage.Neo4j.for_Neo4jSinkProvenanceStorage;

public class when_marking_with_the_protection_marker_hidden : given.a_graph_database
{
    readonly Guid _databaseId = Guid.NewGuid();
    bool _latched;
    Exception? _error;

    void Establish()
    {
        _runtime.RunAsync(Read).Returns(_ => Cursor(Inventory(_databaseId, SinkProvenanceState.CurrentInventoryVersion, _latched)));
        _runtime.RunAsync(LockInventory, Arg.Any<object>()).Returns(_ => Cursor());
        _runtime.RunAsync(Mark, Arg.Any<object>()).Returns(_ => Cursor());
        _runtime.RunAsync(LatchRestriction, Arg.Any<object>()).Returns(_ =>
        {
            _latched = true;
            return Cursor();
        });
    }

    async Task Because() => _error = await Catch.Exception(_provenance.MarkQuarantined);

    [Fact] void should_latch_restriction_on_the_inventory() => _latched.ShouldBeTrue();
    [Fact] void should_confirm_restriction_even_without_seeing_the_protection_marker() => _error.ShouldBeNull();
    [Fact] async Task should_still_write_the_separate_protection_marker() => await _runtime.Received(1).RunAsync(Mark, Arg.Any<object>());
}
