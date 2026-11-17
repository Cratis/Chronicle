// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;
extern alias KernelCore;

using System.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.InMemory.Sinks;
using Cratis.Serialization;
using KernelAppendedEvent = KernelConcepts::Cratis.Chronicle.Concepts.Events.AppendedEvent;
using KernelConceptsNs = KernelConcepts::Cratis.Chronicle.Concepts;
using KernelKey = KernelConcepts::Cratis.Chronicle.Concepts.Keys.Key;
using KernelProjectionEngine = KernelCore::Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Testing.ReadModels;

internal static partial class ProjectionReadModelProcessor
{
    /// <summary>
    /// The state of one fold: the events being folded through a projection, the per-instance states they
    /// produce, and the events whose keys are still waiting for a parent.
    /// </summary>
    sealed class FoldRun
    {
        static readonly (KernelKey? Key, bool Removed) _skipped = (null, false);

        readonly Dictionary<object, ExpandoObject> _statesByKey = [];
        readonly HashSet<object> _seededKeys = [];
        readonly Queue<KernelAppendedEvent> _deferredEvents = new();
        readonly List<(KernelProjectionEngine::IProjection Child, KernelAppendedEvent Event)> _deferredChildren = [];

        /// <summary>
        /// The first non-deferred key resolved for the root projection. In production, MongoDB
        /// upserts each read model document with `_id` = this key value, and the BSON deserializer maps
        /// `_id` back onto the read model's identifier property (the property marked [Key] / [Subject]
        /// or conventionally named "Id"). The in-memory test harness skips the MongoDB round-trip, so
        /// we mirror that mapping ourselves before serializing the state — otherwise identifier
        /// properties whose only source is the event-source ID surface as null in _scenario.Instance.
        /// </summary>
        KernelKey? _rootKey;

        public required KernelProjectionEngine::IProjection Projection { get; init; }

        public required KernelConceptsNs::Projections.Definitions.ProjectionDefinition ProjectionDefinition { get; init; }

        public required IEventSequenceStorage Storage { get; init; }

        public required InMemorySink Sink { get; init; }

        public required Type ReadModelType { get; init; }

        public required Func<ExpandoObject> CreateSeedState { get; init; }

        public required bool StrictEventSubscription { get; init; }

        public required bool ExplicitInitialStateIsPresent { get; set; }

        public bool HasSeededInstances => _seededKeys.Count > 0;

        public void Seed(object key, Func<ExpandoObject> createState)
        {
            var stateKey = Sink.GetKeyValue(new KernelKey(key, ArrayIndexers.NoIndexers));
            _statesByKey[stateKey] = createState();
            Sink.Collection[stateKey] = createState();
            _seededKeys.Add(stateKey);
        }

        /// <summary>
        /// Retry deferred keys at each releasing event, before later seeded key changes can participate.
        /// </summary>
        /// <param name="appendedEvents">The events to fold, in order.</param>
        /// <returns>Awaitable task.</returns>
        public async Task ProcessEvents(IEnumerable<KernelAppendedEvent> appendedEvents)
        {
            foreach (var @event in appendedEvents)
            {
                var (eventKey, eventRemoved) = await ProcessEvent(@event);
                PreserveSeededIdentifiers();
                Record(eventKey, eventRemoved);

                if (eventKey is not null)
                {
                    var (releasedKey, removed) = await ReleaseDeferredEvents(
                        Projection,
                        Storage,
                        Sink,
                        @event,
                        _statesByKey,
                        CreateSeedState,
                        _deferredEvents,
                        _deferredChildren,
                        PreserveSeededIdentifiers);
                    Record(releasedKey, removed);
                }
            }
        }

        public void ThrowIfEventsAreUnresolved()
        {
            if (_deferredEvents.Count > 0)
            {
                var @event = _deferredEvents.Peek();
                throw new InvalidOperationException(
                    $"Could not resolve parent key for event '{@event.Context.EventType.Id}' (EventSourceId '{@event.Context.EventSourceId}'). " +
                    "Ensure the parent event is included in the scenario and appended before the child event.");
            }

            if (_deferredChildren.Count > 0)
            {
                var (child, @event) = _deferredChildren[0];
                throw new InvalidOperationException(
                    $"Could not resolve parent key for child collection '{child.Path}' for event '{@event.Context.EventType.Id}' (EventSourceId '{@event.Context.EventSourceId}'). " +
                    "Ensure the parent event is included in the scenario and appended before the child event.");
            }
        }

        /// <summary>
        /// An explicit initial state remains materialized through empty changesets until an actual root removal.
        /// Otherwise presence comes from the same per-key sink state that backs Instances: deferred, joined, or
        /// unsubscribed no-ops cannot create an id-only model, while a later constructive event recreates it.
        /// </summary>
        public async Task<bool> HasPrimaryInstance() =>
            ExplicitInitialStateIsPresent ||
            (_rootKey is not null && await Sink.FindOrDefault(_rootKey) is not null);

        public ExpandoObject GetPrimaryState()
        {
            var state = _rootKey is not null && _statesByKey.TryGetValue(Sink.GetKeyValue(_rootKey), out var rootState)
                ? rootState
                : CreateSeedState();

            InjectIdentifierFromKey(state, _rootKey, ReadModelType);
            return state;
        }

        void Record(KernelKey? key, bool removed)
        {
            _rootKey ??= key;
            if (removed)
            {
                ExplicitInitialStateIsPresent = false;
            }
        }

        void PreserveSeededIdentifiers()
        {
            var identifier = IdentifierProperty.Find(ReadModelType);
            if (identifier is null || _seededKeys.Count == 0) return;

            // InMemorySink injects id from the storage key on every write. For an explicitly seeded document,
            // keep the identifier held by its projected state instead; it need not equal that storage key.
            var property = AcronymFriendlyJsonCamelCaseNamingPolicy.Instance.ConvertName(identifier.Name);
            foreach (var key in _seededKeys)
            {
                if (TryGetIdentifier(key, property, out var value))
                {
                    ((IDictionary<string, object?>)Sink.Collection[key])[property] = value;
                }
            }
        }

        bool TryGetIdentifier(object key, string property, out object? value)
        {
            value = null;
            return _statesByKey.TryGetValue(key, out var state) &&
                Sink.Collection.ContainsKey(key) &&
                ((IDictionary<string, object?>)state).TryGetValue(property, out value);
        }

        async Task<(KernelKey? Key, bool Removed)> ProcessEvent(KernelAppendedEvent @event)
        {
            if (!IsSubscribedTo(@event))
            {
                return _skipped;
            }

            var keyResult = await ResolveKeyResult(@event);
            if (keyResult is null)
            {
                return _skipped;
            }

            if (keyResult is KernelProjectionEngine::DeferredKey)
            {
                _deferredEvents.Enqueue(@event);
                return _skipped;
            }

            var resolvedKey = (keyResult as KernelProjectionEngine::ResolvedKey)!;
            var key = await AnchorChildJoinKey(@event, resolvedKey.Key);
            if (key is null)
            {
                return _skipped;
            }

            var removed = await ApplyResolvedEvent(Projection, Storage, Sink, @event, key, _statesByKey, CreateSeedState, _deferredChildren, resolvedKey.JoinKey);
            return (key, removed);
        }

        /// <summary>
        /// The production projection engine filters the event stream to the types the projection
        /// subscribes to, so an event a projection does not handle never reaches it. A seeded stream
        /// may legitimately carry such unrelated events (e.g. an audit/marker event); skip them here
        /// rather than letting GetKeyResolverFor throw MissingKeyResolverForEventType. HasKeyResolverFor
        /// is the exact predicate for "would GetKeyResolverFor succeed" (same resolver map), and the
        /// root projection's map includes child ([ChildrenFrom]) event types, so this is a faithful
        /// no-op filter for nested/join scenarios too. In strict mode the same condition is surfaced as a
        /// loud error instead, so a spec that opted in can catch the genuine mistake of seeding the wrong event.
        /// </summary>
        /// <param name="event">The event to check.</param>
        /// <returns>True when the projection handles the event.</returns>
        /// <exception cref="UnsubscribedEventSeeded">Thrown in strict mode when the event is not subscribed to.</exception>
        bool IsSubscribedTo(KernelAppendedEvent @event)
        {
            if (Projection.HasKeyResolverFor(@event.Context.EventType))
            {
                return true;
            }

            if (StrictEventSubscription)
            {
                throw new UnsubscribedEventSeeded(@event.Context.EventType.Id.ToString());
            }

            return false;
        }

        /// <summary>
        /// A root-level [Join] source enriches an existing root document; use the kernel resolver and skip
        /// unmatched roots so the in-memory sink doesn't materialize a phantom (see JoinKeyResolver). A null
        /// root means the row doesn't exist yet — skip; ResolveJoin backfills when the FROM event arrives.
        /// A null result means the event is skipped.
        /// </summary>
        /// <param name="event">The event to resolve a key for.</param>
        /// <returns>The resolved key result, or null when the event is skipped.</returns>
        async Task<KernelProjectionEngine::KeyResolverResult?> ResolveKeyResult(KernelAppendedEvent @event)
        {
            var (isRootJoinSource, resolvedRootKey, joinKey) = await JoinKeyResolver.TryResolveRootJoinKey(Projection, ProjectionDefinition, Storage, Sink, @event);
            if (isRootJoinSource)
            {
                // The resolved root key identifies the row; the join matches on the value resolved from its key expression.
                // Preserve both, just as ForJoin does in the production ResolveKey step.
                return resolvedRootKey is null
                    ? null
                    : KernelProjectionEngine::KeyResolverResult.Resolved(resolvedRootKey, joinKey);
            }

            try
            {
                return await Projection.GetKeyResolverFor(@event.Context.EventType)(Storage, Sink, @event);
            }
            catch (FormatException) when (Projection.GetOperationTypeFor(@event.Context.EventType).HasFlag(KernelProjectionEngine::ProjectionOperationType.Join))
            {
                // Defensive fallback for a nested/child [Join] whose join key can't be force-converted to the
                // identifier type: skip rather than crash the scenario (root joins are handled above).
                return null;
            }
        }

        /// <summary>
        /// A child-level [Join] source resolves onto a child of an existing root; re-anchor onto that root
        /// (keeping the array indexers) so the child is updated in place rather than duplicated into a phantom
        /// (see JoinKeyResolver). A null root means the child doesn't exist yet — skip; ResolveJoin backfills it.
        /// A null result means the event is skipped.
        /// </summary>
        /// <param name="event">The event being applied.</param>
        /// <param name="key">The resolved key.</param>
        /// <returns>The key to apply the event under, or null when the event is skipped.</returns>
        async Task<KernelKey?> AnchorChildJoinKey(KernelAppendedEvent @event, KernelKey key)
        {
            var (isChildJoinSource, childJoinRootKey) = await JoinKeyResolver.TryResolveChildJoinRootKey(Projection, Sink, @event, key);
            if (!isChildJoinSource)
            {
                return key;
            }

            return childJoinRootKey is null ? null : key with { Value = childJoinRootKey.Value };
        }
    }
}
