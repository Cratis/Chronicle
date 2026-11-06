// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Serialization;

namespace Cratis.Chronicle.Integration.GraphProjections;

/// <summary>
/// Represents the base of a graph projection specification: a specification that starts on an empty Neo4j graph and reads the result back
/// from Neo4j with Cypher of its own.
/// </summary>
/// <param name="fixture">The <see cref="GraphChronicleFixture"/>.</param>
public class GraphSpecification(GraphChronicleFixture fixture) : Specification<GraphChronicleFixture>(fixture)
{
    /// <summary>
    /// The longest a specification waits for the graph to show what the events left.
    /// </summary>
    protected static readonly TimeSpan GraphTimeout = TimeSpanFactory.FromSeconds(60);

    /// <summary>
    /// Gets the graph, as read after the last event was appended and observed.
    /// </summary>
    public GraphSnapshot Graph { get; private set; } = new([], []);

    /// <summary>
    /// Gets the outcome of waiting for the observers after the last event was appended.
    /// </summary>
    public AppendResultWaitForCompletionResult? LastCompletion { get; private set; }

    /// <inheritdoc/>
    public override IReadOnlyDictionary<string, string?>? AdditionalHostConfiguration => ChronicleFixture.HostConfiguration;

    /// <summary>
    /// Gets the services registered in the host. The graph specifications name properties the way an Arc application does, in camel case, which is also how a
    /// declaration in the projection declaration language has to spell them.
    /// </summary>
    public override Action<IServiceCollection>? ConfigureHostServices => services => services.AddSingleton<INamingPolicy>(new CamelCaseNaming());

    /// <summary>
    /// Gets a value indicating whether the sink keeps every namespace in one database, which is the only mode a Community server supports.
    /// </summary>
    public bool IsSingleDatabase => ChronicleFixture.DatabaseMode == "Single";

    /// <summary>
    /// Gets the Neo4j server.
    /// </summary>
    protected Neo4jGraphServer Server => ChronicleFixture.GraphServer;

    /// <summary>
    /// Append an event to the event log and wait for the observers, the graph projections among them, to be done with it.
    /// </summary>
    /// <param name="eventSourceId">The event source to append to.</param>
    /// <param name="event">The event to append.</param>
    /// <param name="occurred">Optional time the event occurred.</param>
    /// <param name="expectFailure">Whether an observer is expected to fail on the event, for instance because the sink refuses it.</param>
    /// <returns>The sequence number of the event.</returns>
    protected async Task<ulong> Append(EventSourceId eventSourceId, object @event, DateTimeOffset? occurred = null, bool expectFailure = false)
    {
        var result = await EventStore.EventLog.Append(eventSourceId, @event, occurred: occurred);
        result.IsSuccess.ShouldBeTrue();
        LastCompletion = await result.WaitForCompletion(TimeSpan.FromSeconds(60));
        LastCompletion.IsSuccess.ShouldEqual(!expectFailure);
        return result.SequenceNumber.Value;
    }

    /// <summary>
    /// Read the graph from Neo4j into <see cref="Graph"/>, waiting until a condition on it holds or the timeout passes.
    /// </summary>
    /// <param name="condition">Optional condition the graph must meet. Without one the graph is read once.</param>
    /// <returns>Awaitable task.</returns>
    protected async Task ReadGraph(Func<GraphSnapshot, bool>? condition = null) => await ReadGraph(ChronicleFixture.Database, condition);

    /// <summary>
    /// Read a database of the Neo4j server into <see cref="Graph"/>, waiting until a condition on it holds or the timeout passes.
    /// </summary>
    /// <param name="database">The name of the database.</param>
    /// <param name="condition">Optional condition the graph must meet. Without one the graph is read once.</param>
    /// <returns>Awaitable task.</returns>
    protected async Task ReadGraph(string database, Func<GraphSnapshot, bool>? condition)
    {
        var deadline = DateTime.UtcNow.Add(GraphTimeout);
        while (true)
        {
            Graph = await GraphSnapshot.Read(Server.Driver, database);
            if (condition is null || condition(Graph) || DateTime.UtcNow >= deadline)
            {
                return;
            }

            await Task.Delay(100);
        }
    }

    async Task Establish() => await Server.Clear();
}
