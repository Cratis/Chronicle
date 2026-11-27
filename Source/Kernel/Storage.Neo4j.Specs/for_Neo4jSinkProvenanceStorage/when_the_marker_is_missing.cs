// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;
using static Cratis.Chronicle.Storage.Neo4j.Neo4jSinkProvenanceStatements;

namespace Cratis.Chronicle.Storage.Neo4j.for_Neo4jSinkProvenanceStorage;

public class when_the_marker_is_missing : given.a_graph_database
{
    readonly Guid _winner = Guid.NewGuid();
    SinkProvenanceState _state;

    void Establish()
    {
        _runtime.RunAsync(Read).Returns(_ => Cursor(), _ => Cursor(Inventory(_winner)));
        _runtime.RunAsync(Seed, Arg.Any<object>()).Returns(_ => Cursor());
    }

    async Task Because() => _state = await _provenance.ReadState();

    [Fact] void should_not_publish_inventory_completion() => _state.IsComplete.ShouldBeFalse();
    [Fact] void should_read_back_the_committed_identity() => _state.DatabaseId.Value.ShouldEqual(_winner);
    [Fact] async Task should_not_seed_state_with_schema_credentials() => await _schema.DidNotReceive().RunAsync(Seed, Arg.Any<object>());
}
