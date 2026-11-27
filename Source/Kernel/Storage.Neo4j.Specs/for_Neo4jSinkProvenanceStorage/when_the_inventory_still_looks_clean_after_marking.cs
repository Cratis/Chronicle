// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;
using static Cratis.Chronicle.Storage.Neo4j.Neo4jSinkProvenanceStatements;

namespace Cratis.Chronicle.Storage.Neo4j.for_Neo4jSinkProvenanceStorage;

public class when_the_inventory_still_looks_clean_after_marking : given.a_graph_database
{
    readonly Guid _databaseId = Guid.NewGuid();
    Exception? _error;

    void Establish()
    {
        _runtime.RunAsync(Read).Returns(_ => Cursor(Inventory(_databaseId, SinkProvenanceState.CurrentInventoryVersion)));
        _runtime.RunAsync(Lock, Arg.Any<object>()).Returns(_ => Cursor());
        _runtime.RunAsync(Mark, Arg.Any<object>()).Returns(_ => Cursor());
        _runtime.RunAsync(LatchRestriction, Arg.Any<object>()).Returns(_ => Cursor());
    }

    async Task Because() => _error = await Catch.Exception(_provenance.MarkQuarantined);

    [Fact] void should_refuse_to_report_success() => _error.ShouldBeOfExactType<InvalidOperationException>();
}
