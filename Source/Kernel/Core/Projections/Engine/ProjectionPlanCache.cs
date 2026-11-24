// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Holds the <see cref="ProjectionPlan">plans</see> of a silo, one per event store, projection and definition fingerprint.
/// </summary>
/// <remarks>
/// <para>
/// The cache is bounded by the number of projections, never by the number of namespaces. For every projection it keeps the plan of the
/// current definition and the one before it, so a definition that is read two ways at once (for instance an observer and a read, which
/// may differ in generation delivery) does not rebuild on every call and a definition change does not leave plans behind.
/// </para>
/// <para>
/// A plan is valid for as long as the read model definition and the schemas of the event types it takes part in are the ones it was
/// compiled from. The schemas are checked cheaply through the revision of the event type registry, and by identity when no
/// revision is available.
/// </para>
/// </remarks>
internal sealed class ProjectionPlanCache
{
    const int PlansPerProjection = 2;

    readonly Lock _lock = new();
    readonly Dictionary<(EventStoreName Store, ProjectionId Id, string Fingerprint), Entry> _entries = [];
    long _sequence;

    /// <summary>
    /// Gets the number of plans held.
    /// </summary>
    internal int Count
    {
        get
        {
            lock (_lock) return _entries.Count;
        }
    }

    /// <summary>
    /// Gets the number of plans held for one projection.
    /// </summary>
    /// <param name="store">The <see cref="EventStoreName"/>.</param>
    /// <param name="id">The <see cref="ProjectionId"/>.</param>
    /// <returns>The number of plans.</returns>
    internal int CountFor(EventStoreName store, ProjectionId id)
    {
        lock (_lock) return _entries.Keys.Count(key => key.Store == store && key.Id == id);
    }

    /// <summary>
    /// Gets a plan for a definition, compiling it when no valid one is held.
    /// </summary>
    /// <param name="store">The <see cref="EventStoreName"/>.</param>
    /// <param name="id">The <see cref="ProjectionId"/>.</param>
    /// <param name="fingerprint">The fingerprint of the definition.</param>
    /// <param name="readModelFingerprint">The fingerprint of the read model definition.</param>
    /// <param name="revision">The revision of the event type registry, or null when it is not known.</param>
    /// <param name="loadSchemas">Loads the schemas of the participating event types.</param>
    /// <param name="compile">Compiles a plan from the schemas of the participating event types.</param>
    /// <returns>The shared <see cref="ProjectionPlan"/>.</returns>
    internal async Task<ProjectionPlan> GetOrCompile(
        EventStoreName store,
        ProjectionId id,
        string fingerprint,
        string readModelFingerprint,
        long? revision,
        Func<Task<IReadOnlyList<EventTypeSchema>>> loadSchemas,
        Func<IReadOnlyList<EventTypeSchema>, ProjectionPlan> compile)
    {
        var key = (store, id, fingerprint);
        Entry? entry;
        lock (_lock)
        {
            _entries.TryGetValue(key, out entry);
        }

        if (entry is not null && entry.Plan.ReadModelFingerprint == readModelFingerprint && revision is not null && entry.Revision == revision)
        {
            return entry.Plan;
        }

        // Either nothing is held, something about the read model changed, or the registry has moved on (or cannot say). The registry moves on
        // for any event type of the store, so look at what this projection actually takes part in before throwing the plan away.
        var schemas = await loadSchemas();
        lock (_lock)
        {
            _entries.TryGetValue(key, out entry);
            if (entry is not null && entry.Plan.ReadModelFingerprint == readModelFingerprint && entry.Plan.IsCompiledFrom(schemas))
            {
                entry.Revision = revision;
                return entry.Plan;
            }

            var plan = compile(schemas);
            _entries[key] = new Entry(plan, revision, ++_sequence);
            Trim(store, id);
            return plan;
        }
    }

    /// <summary>
    /// Forgets every plan of a projection.
    /// </summary>
    /// <param name="store">The <see cref="EventStoreName"/>.</param>
    /// <param name="id">The <see cref="ProjectionId"/>.</param>
    internal void Remove(EventStoreName store, ProjectionId id)
    {
        lock (_lock)
        {
            foreach (var key in _entries.Keys.Where(key => key.Store == store && key.Id == id).ToArray())
            {
                _entries.Remove(key);
            }
        }
    }

    void Trim(EventStoreName store, ProjectionId id)
    {
        var held = _entries.Where(entry => entry.Key.Store == store && entry.Key.Id == id).OrderByDescending(entry => entry.Value.Sequence).ToArray();
        foreach (var stale in held.Skip(PlansPerProjection))
        {
            _entries.Remove(stale.Key);
        }
    }

    sealed class Entry(ProjectionPlan plan, long? revision, long sequence)
    {
        public ProjectionPlan Plan { get; } = plan;

        public long? Revision { get; set; } = revision;

        public long Sequence { get; } = sequence;
    }
}
