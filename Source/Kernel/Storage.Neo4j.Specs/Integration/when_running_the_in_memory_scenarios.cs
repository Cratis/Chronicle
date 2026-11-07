// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;
using Cratis.Chronicle.Storage.InMemory.Sinks.Graphs;

namespace Cratis.Chronicle.Storage.Neo4j.Integration;

/// <summary>
/// Runs every scenario of the in-memory graph sink specifications against a live Neo4j server and against the in-memory
/// sink, and requires the same graph, and the same observations along the way, from both.
/// </summary>
public class when_running_the_in_memory_scenarios : Specification
{
    readonly Neo4jDriverFactory _driverFactory = new();
    readonly Dictionary<string, string> _reference = [];
    readonly Dictionary<string, string> _live = [];
    readonly List<string> _namespaces = [];

    async Task Because()
    {
        if (!LiveNeo4j.Enabled) return;

        var connections = LiveNeo4j.CreateConnections(_driverFactory);
        foreach (var (name, scenario) in GraphScenarios.All)
        {
            _reference[name] = await Run(scenario, new InMemoryGraphSink(GraphDefinitions.Organization));

            var @namespace = $"{LiveNeo4j.NamespacePrefix}{Guid.NewGuid():N}";
            _namespaces.Add(@namespace);
            var sink = new Neo4jGraphSink(GraphDefinitions.Organization, "specs", @namespace, connections);
            await sink.EnsureSchema();
            _live[name] = await Run(scenario, sink);
        }
    }

    async Task Destroy()
    {
        if (LiveNeo4j.Enabled) await LiveNeo4j.Clean(_driverFactory, _namespaces);
        await _driverFactory.DisposeAsync();
    }

    static async Task<string> Run(Func<GraphScenarioRun, Task> scenario, IGraphSink sink)
    {
        var run = new GraphScenarioRun(sink);
        await scenario(run);
        return string.Join(Environment.NewLine, run.Observations.Concat(await GraphDump.Describe(sink)));
    }

    [LiveNeo4jFact] void should_run_every_scenario() => _live.Count.ShouldEqual(GraphScenarios.All.Count);

    [LiveNeo4jFact] void should_have_something_to_compare_in_every_scenario() =>
        _reference.Where(_ => !_.Value.Contains("node ", StringComparison.Ordinal) && !_.Value.Contains("edge ", StringComparison.Ordinal) && !_.Value.Contains("refused: ", StringComparison.Ordinal)).Select(_ => _.Key).ShouldBeEmpty();

    [LiveNeo4jFact] void should_get_the_same_graph_and_observations_as_the_in_memory_sink() =>
        GraphScenarios.All.Keys.Where(_ => _reference[_] != _live[_]).Select(_ => $"{_}{Environment.NewLine}--- in-memory{Environment.NewLine}{_reference[_]}{Environment.NewLine}--- neo4j{Environment.NewLine}{_live[_]}").ShouldBeEmpty();
}
