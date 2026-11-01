// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_Sink.given;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_finding_a_root_key_in_bulk.given;

public abstract class a_bulk_sink_with_children(ReplicaSetMongoDBFixture fixture) : a_capped_fenced_sink(fixture)
{
    protected readonly Key _key = new("root", ArrayIndexers.NoIndexers);

    async Task Establish()
    {
        await CreateSink(
            """
            {"type":"object","properties":{"id":{"type":"string"},"count":{"type":"integer"},"children":{"type":"array","items":{"type":"object","properties":{"childId":{"type":"string"}}}}}}
            """,
            cap: 100);
        await _sink.BeginBulk();
    }

    protected static ExpandoObject Child(string id)
    {
        var child = new ExpandoObject();
        ((IDictionary<string, object?>)child)["childId"] = id;
        return child;
    }

    protected static ExpandoObject RootWith(params string[] childIds)
    {
        var root = new ExpandoObject();
        ((IDictionary<string, object?>)root)["children"] = childIds.Select(id => (object)Child(id)).ToList();
        return root;
    }

    protected static ArrayIndexers IndexerFor(string id) => new([new ArrayIndexer("children", "childId", id)]);

    protected static IChangeset<AppendedEvent, ExpandoObject> ChangesWithState(ExpandoObject state, params Change[] changes)
    {
        var changeset = Changes(changes);
        changeset.InitialState.Returns(state);
        return changeset;
    }
}
