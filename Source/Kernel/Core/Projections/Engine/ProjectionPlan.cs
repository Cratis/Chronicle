// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Everything about a compiled projection that does not depend on the namespace it runs in: the normalized definition,
/// the read model and its schema, the schemas of the event types the projection takes part in, and the date time handling
/// for those.
/// </summary>
/// <remarks>
/// <para>
/// A plan is a pure function of the stored definition, the read model definition and the schemas of the participating event types. It is built
/// once per event store and silo and shared by every namespace, so a projection costs the same whether it runs in one namespace or in
/// hundreds. What each namespace owns is a run: the <see cref="Projection"/> with its own Rx subject graph, its own initial state and,
/// through the namespace's event sequence storage, its own join resolution. Nothing a run mutates lives in the plan.
/// </para>
/// <para>
/// The plan itself never changes after it is built. The only thing it accumulates is a memo of the schemas of pinned event type generations,
/// which is idempotent: resolving the same generation twice yields the same schema, so the first resolution wins and every later run reuses it.
/// Property mappers are deliberately not shared: resolving one can mark the read model schema it is for, and each run does that itself.
/// </para>
/// </remarks>
public sealed class ProjectionPlan
{
    readonly HashSet<EventTypeSchema> _schemaSet;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionPlan"/> class.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the plan is compiled for.</param>
    /// <param name="definition">The normalized <see cref="ProjectionDefinition"/>.</param>
    /// <param name="readModel">The <see cref="ReadModelDefinition"/> the projection targets.</param>
    /// <param name="fingerprint">The fingerprint of the definition as it was stored.</param>
    /// <param name="readModelFingerprint">The fingerprint of the read model definition.</param>
    /// <param name="schemas">The schemas of the event types the projection takes part in.</param>
    internal ProjectionPlan(
        EventStoreName eventStore,
        ProjectionDefinition definition,
        ReadModelDefinition readModel,
        string fingerprint,
        string readModelFingerprint,
        IReadOnlyList<EventTypeSchema> schemas)
    {
        EventStore = eventStore;
        Definition = definition;
        ReadModel = readModel;
        Fingerprint = fingerprint;
        ReadModelFingerprint = readModelFingerprint;
        Schemas = schemas;
        _schemaSet = new(schemas, ReferenceEqualityComparer.Instance);
        DateTimes = new(schemas);
    }

    /// <summary>
    /// Gets the <see cref="EventStoreName"/> the plan is compiled for.
    /// </summary>
    public EventStoreName EventStore { get; }

    /// <summary>
    /// Gets the normalized <see cref="ProjectionDefinition"/> the runs are wired from.
    /// </summary>
    public ProjectionDefinition Definition { get; }

    /// <summary>
    /// Gets the <see cref="ReadModelDefinition"/> the projection targets.
    /// </summary>
    public ReadModelDefinition ReadModel { get; }

    /// <summary>
    /// Gets the fingerprint of the definition as it was stored.
    /// </summary>
    public string Fingerprint { get; }

    /// <summary>
    /// Gets the fingerprint of the read model definition the plan was compiled against.
    /// </summary>
    public string ReadModelFingerprint { get; }

    /// <summary>
    /// Gets the schemas of the event types the projection takes part in, including those of its children, nested objects and joins.
    /// </summary>
    public IReadOnlyList<EventTypeSchema> Schemas { get; }

    /// <summary>
    /// Gets the <see cref="ProjectionDateTimes"/> limited to the event types the projection takes part in.
    /// </summary>
    public ProjectionDateTimes DateTimes { get; }

    /// <summary>
    /// Gets the schemas of the pinned event type generations the projection has resolved so far.
    /// </summary>
    internal ConcurrentDictionary<EventType, EventTypeSchema> PinnedSchemas { get; } = new();

    /// <summary>
    /// Gets whether the plan was compiled from exactly these schemas, compared by identity.
    /// </summary>
    /// <param name="schemas">The schemas of the participating event types as they are now.</param>
    /// <returns>True when nothing the plan was compiled from has changed.</returns>
    internal bool IsCompiledFrom(IReadOnlyList<EventTypeSchema> schemas) =>
        schemas.Count == Schemas.Count && schemas.All(_schemaSet.Contains);
}
