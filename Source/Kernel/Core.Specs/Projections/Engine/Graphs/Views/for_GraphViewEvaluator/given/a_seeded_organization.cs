// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;
using Cratis.Chronicle.Storage.InMemory.Sinks.Graphs;

namespace Cratis.Chronicle.Projections.Engine.Graphs.Views.for_GraphViewEvaluator.given;

/// <summary>
/// An in-memory graph seeded with the nodes and edges of the shared query vectors.
/// </summary>
public class a_seeded_organization : Views.given.an_organization_definition
{
    protected InMemoryGraphSink _sink;

    async Task Establish()
    {
        _sink = new InMemoryGraphSink(_graph);

        var sequence = 0UL;
        foreach (var node in Vectors["nodes"]!.AsArray())
        {
            var reference = new NodeReference(node!["label"]!.GetValue<string>(), node["key"]!.GetValue<string>());
            await _sink.ApplyGraphChanges(new("seed", [new UpsertNode(reference, PropertiesOf(node["properties"]))]), ++sequence);
        }

        foreach (var edge in Vectors["edges"]!.AsArray())
        {
            var reference = new EdgeReference(
                edge!["type"]!.GetValue<string>(),
                new(edge["source"]!["label"]!.GetValue<string>(), edge["source"]!["key"]!.GetValue<string>()),
                new(edge["target"]!["label"]!.GetValue<string>(), edge["target"]!["key"]!.GetValue<string>()),
                ValueOf(edge["identity"]));
            await _sink.ApplyGraphChanges(new("seed", [new UpsertEdge(reference, PropertiesOf(edge["properties"]))]), ++sequence);
        }
    }
}
