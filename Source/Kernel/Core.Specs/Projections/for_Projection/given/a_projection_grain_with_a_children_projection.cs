// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Engine.Expressions;
using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Projections.Engine.Expressions.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventSequences;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orleans.Core;
using Orleans.TestKit;

namespace Cratis.Chronicle.Projections.for_Projection.given;

/// <summary>
/// A <see cref="Projection"/> grain running the real projection engine for a read model whose only content is a
/// collection of children - the shape of an on-demand (passive) read model built from children alone.
/// </summary>
public class a_projection_grain_with_a_children_projection : Specification
{
    protected const string EventStore = "the-event-store";
    protected const string ReadModel = "Comments.Associations";

    protected static readonly EventType _associated = new("CommentAssociated", EventTypeGeneration.First);

    protected Projection _grain;
    protected TestKitSilo _silo;

    async Task Establish()
    {
        _silo = new TestKitSilo();

        var typeFormats = new TypeFormats();
        var keyResolvers = new KeyResolvers(NullLogger<KeyResolvers>.Instance);
        var eventValueProviderExpressionResolvers = new EventValueProviderExpressionResolvers(typeFormats, NullLogger<EventValueProviderExpressionResolvers>.Instance);

        var storage = Substitute.For<Storage.IStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).ReadModels.GetAll().Returns([]);
        storage.GetEventStore(Arg.Any<EventStoreName>()).EventTypes.GetLatestForAllEventTypes().Returns([]);
        storage.GetEventStore(Arg.Any<EventStoreName>())
            .GetNamespace(Arg.Any<EventStoreNamespaceName>())
            .GetEventSequence(Arg.Any<EventSequenceId>())
            .Returns(Substitute.For<IEventSequenceStorage>());

        _silo.AddService<IProjectionFactory>(new ProjectionFactory(
            new ReadModelPropertyExpressionResolvers(eventValueProviderExpressionResolvers, typeFormats, NullLogger<ReadModelPropertyExpressionResolvers>.Instance),
            eventValueProviderExpressionResolvers,
            new KeyExpressionResolvers(eventValueProviderExpressionResolvers, keyResolvers, NullLogger<KeyExpressionResolvers>.Instance),
            new ExpandoObjectConverter(typeFormats),
            keyResolvers,
            storage,
            NullLogger<ProjectionFactory>.Instance));
        _silo.AddService(Substitute.For<IProjectionDefinitionComparer>());
        _silo.AddService<IObjectComparer>(new ObjectComparer());
        _silo.AddService(storage);
        _silo.AddService(Options.Create(new ChronicleOptions()));

        var namespacesGrain = Substitute.For<INamespaces>();
        namespacesGrain.GetAll().Returns([]);
        _silo.AddProbe(_ => namespacesGrain);

        var readModelGrain = Substitute.For<IReadModel>();
        readModelGrain.GetDefinition().Returns(Task.FromResult(CreateReadModelDefinition()));
        _silo.AddProbe(_ => readModelGrain);

        var stateStorage = Substitute.For<IStorage<ProjectionDefinition>>();
        stateStorage.State = CreateDefinition();
        _silo.Options.StorageFactory = _ => stateStorage;

        _grain = await _silo.CreateGrainAsync<Projection>(new ProjectionKey(ReadModel, EventStore).ToString());
    }

    /// <summary>
    /// Folds the events for one event source into a read model, from nothing, the way an on-demand read does.
    /// </summary>
    /// <param name="events">The events to fold.</param>
    /// <returns>The read model; empty when there is none.</returns>
    protected Task<ExpandoObject> Fold(params AppendedEvent[] events) => FoldOnto(new ExpandoObject(), events);

    /// <summary>
    /// Folds the events for one event source onto a read model already folded, the way an on-demand read
    /// continues from the state it cached.
    /// </summary>
    /// <param name="state">The state already folded.</param>
    /// <param name="events">The events to fold.</param>
    /// <returns>The read model; empty when there is none.</returns>
    protected Task<ExpandoObject> FoldOnto(ExpandoObject state, params AppendedEvent[] events) =>
        _grain.ProcessForSingleReadModel(EventStoreNamespaceName.Default, state, events);

    /// <summary>
    /// Creates the event associating a comment with the provider's reference for it.
    /// </summary>
    /// <param name="eventSourceId">The event source the association is for.</param>
    /// <param name="sequenceNumber">The event's sequence number.</param>
    /// <param name="comment">The comment.</param>
    /// <param name="reference">The provider's reference.</param>
    /// <returns>The appended event.</returns>
    protected static AppendedEvent Associated(EventSourceId eventSourceId, ulong sequenceNumber, string comment, string reference) =>
        new(
            EventContext.EmptyWithEventSourceId(eventSourceId) with { EventType = _associated, SequenceNumber = sequenceNumber },
            new { comment, provider = 1, reference }.AsExpandoObject());

    /// <summary>
    /// Gets the children of a folded read model.
    /// </summary>
    /// <param name="readModel">The read model.</param>
    /// <returns>The children as dictionaries.</returns>
    protected static IEnumerable<IDictionary<string, object?>> AssociationsOf(ExpandoObject readModel) =>
        ((IDictionary<string, object?>)readModel).TryGetValue("associations", out var items) && items is IEnumerable<object> children
            ? children.Cast<IDictionary<string, object?>>()
            : [];

    static ProjectionDefinition CreateDefinition() => new(
        ProjectionOwner.Client,
        EventSequenceId.Log,
        ReadModel,
        ReadModel,
        false,
        true,
        new(),
        new Dictionary<EventType, FromDefinition>(),
        new Dictionary<EventType, JoinDefinition>(),
        new Dictionary<PropertyPath, ChildrenDefinition>
        {
            ["associations"] = new(
                "id",
                new Dictionary<EventType, FromDefinition>
                {
                    [_associated] = new(new Dictionary<PropertyPath, string> { ["id"] = "comment", ["provider"] = "provider", ["reference"] = "reference" }, "comment", WellKnownExpressions.EventSourceId)
                },
                new Dictionary<EventType, JoinDefinition>(),
                new Dictionary<PropertyPath, ChildrenDefinition>(),
                new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
                new Dictionary<EventType, RemovedWithDefinition>(),
                new Dictionary<EventType, RemovedWithJoinDefinition>(),
                AutoMap: AutoMap.Enabled)
        },
        [],
        new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
        new Dictionary<EventType, RemovedWithDefinition>(),
        new Dictionary<EventType, RemovedWithJoinDefinition>(),
        AutoMap: AutoMap.Enabled);

    static ReadModelDefinition CreateReadModelDefinition() => new(
        ReadModel,
        "associations",
        "Associations",
        ReadModelOwner.Client,
        ReadModelSource.Code,
        ReadModelObserverType.Projection,
        ReadModelObserverIdentifier.Unspecified,
        SinkDefinition.None,
        new Dictionary<ReadModelGeneration, JsonSchema>
        {
            [ReadModelGeneration.First] = JsonSchema.FromJson("""
                {
                  "type": "object",
                  "properties": {
                    "id": { "type": "string" },
                    "associations": {
                      "type": "array",
                      "items": {
                        "type": "object",
                        "properties": {
                          "id": { "type": "string" },
                          "provider": { "type": "integer" },
                          "reference": { "type": "string" }
                        }
                      }
                    }
                  }
                }
                """)
        },
        []);
}
