// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Globalization;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Monads;
using Cratis.Reflection;
using Cratis.Types;

namespace Cratis.Chronicle.Storage.InMemory.Sinks;

/// <summary>
/// Represents an implementation of <see cref="ISink"/> for working with projections in memory.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="InMemorySink"/> class.
/// </remarks>
/// <param name="readModel">The target <see cref="ReadModelDefinition"/>.</param>
/// <param name="typeFormats">The <see cref="ITypeFormats"/> for resolving actual types from JSON schema.</param>
public partial class InMemorySink(
    ReadModelDefinition readModel,
    ITypeFormats typeFormats) : ISink, ISinkCompositeKeyLookup, IDisposable
{
    readonly Dictionary<object, ExpandoObject> _collection = [];
    readonly Dictionary<object, ExpandoObject> _rewindCollection = [];
    readonly Dictionary<object, ulong> _lastHandledEventSequenceNumbers = [];
    readonly Dictionary<object, ulong> _rewindLastHandledEventSequenceNumbers = [];
    readonly Subject<object> _changeSubject = new();
    readonly Lock _collectionLock = new();
    readonly Type? _keyTargetType = readModel.GetSchemaForLatestGeneration().GetTargetTypeForPropertyPath("id", typeFormats);
    bool _isReplaying;

    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.InMemory;

    /// <summary>
    /// Gets the current collection for the sink represented as a key value of key to <see cref="ExpandoObject"/>.
    /// </summary>
    public IDictionary<object, ExpandoObject> Collection => _isReplaying ? _rewindCollection : _collection;

    /// <summary>
    /// Gets the last handled event sequence number per read model key for the active collection.
    /// </summary>
    /// <remarks>
    /// Held beside the documents rather than on them so the shape the sink hands back to readers is unchanged.
    /// The persistent sinks keep the same value in a sink-owned system property that their read paths strip out
    /// through the read model schema, so a caller observes the same thing either way.
    /// </remarks>
    Dictionary<object, ulong> LastHandledEventSequenceNumbers =>
        _isReplaying ? _rewindLastHandledEventSequenceNumbers : _lastHandledEventSequenceNumbers;

    /// <summary>
    /// Gets the value this sink addresses a document by for a given <see cref="Key"/>.
    /// </summary>
    /// <param name="key">The <see cref="Key"/> to resolve.</param>
    /// <returns>The value the document for <paramref name="key"/> is stored under in <see cref="Collection"/>.</returns>
    /// <exception cref="ReadModelKeyHasNoValue">The key has no value.</exception>
    /// <remarks>
    /// Two <see cref="Key"/> instances addressing the same document — a concept and its underlying primitive,
    /// say — resolve to the same value here. Callers that keep per-document state beside the sink must derive
    /// it from this rather than from <see cref="Key.Value"/>, or the two disagree on what "the same document" is.
    /// </remarks>
    public object GetKeyValue(Key key)
    {
        if (key.Value is null)
        {
            throw new ReadModelKeyHasNoValue(readModel.Identifier);
        }

        if (key.Value is ExpandoObject expandoKey)
        {
            return CompositeKeyEncoding.Encode(expandoKey);
        }

        if (_keyTargetType is not null)
        {
            return TypeConversion.Convert(_keyTargetType, key.Value);
        }

        if (key.Value.IsConcept())
        {
            return key.Value.GetConceptValue();
        }

        if (!key.Value.GetType().IsAPrimitiveType())
        {
            return key.Value.AsExpandoObject(true);
        }

        return key.Value;
    }

    /// <summary>
    /// Remove any existing read model by the given key.
    /// </summary>
    /// <param name="key"><see cref="Key"/> for the read model to remove.</param>
    public void RemoveAnyExisting(Key key)
    {
        var keyValue = GetKeyValue(key);
        lock (_collectionLock)
        {
            Collection.Remove(keyValue);
            LastHandledEventSequenceNumbers.Remove(keyValue);
        }
    }

    /// <inheritdoc/>
    public Task<ExpandoObject?> FindOrDefault(Key key)
    {
        var keyValue = GetKeyValue(key);
        lock (_collectionLock)
        {
            if (Collection.TryGetValue(keyValue, out var value)) return Task.FromResult<ExpandoObject?>(value);
        }

        return Task.FromResult<ExpandoObject?>(null);
    }

    /// <inheritdoc/>
    public Task<ExpandoObject?> FindInPrimaryOrDefault(Key key)
    {
        var keyValue = GetKeyValue(key);
        lock (_collectionLock)
        {
            return Task.FromResult(_collection.GetValueOrDefault(keyValue));
        }
    }

    /// <inheritdoc/>
    public async Task<CompositeKeyLookupResult> FindByCompositeKey(Key key)
    {
        var instance = await FindInPrimaryOrDefault(key);
        return instance is null ? CompositeKeyLookupResult.NotFound : CompositeKeyLookupResult.Found(instance);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<FailedPartition>> ApplyChanges(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset, EventSequenceNumber eventSequenceNumber) =>
        ApplyChanges(key, changeset, eventSequenceNumber, SinkWriteMode.Always);

    /// <inheritdoc/>
    public Task<IEnumerable<FailedPartition>> ApplyChanges(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset, EventSequenceNumber eventSequenceNumber, SinkWriteMode mode)
    {
        // Every row the update touches is evaluated as a private candidate first, so an evaluation failure leaves the
        // published rows and their watermarks exactly as they were and a retry of the same sequence number is admitted.
        // Only completed candidates are published, together, and observers are told once the lock is released.
        StagedWrite staged;
        bool wrotePrimary;
        lock (_collectionLock)
        {
            staged = Stage(key, changeset, eventSequenceNumber, mode);

            // Decided together with the write: reading the flag after the lock is released could see a replay
            // that began in between and drop the announcement of a write that did reach the primary collection.
            wrotePrimary = !_isReplaying;
            Publish(staged, eventSequenceNumber);
        }

        foreach (var changedKey in staged.ChangedKeys)
        {
            NotifyChanged(changedKey, wrotePrimary);
        }

        return Task.FromResult<IEnumerable<FailedPartition>>([]);
    }

    /// <inheritdoc/>
    public Task BeginBulk() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<IEnumerable<FailedPartition>> EndBulk() => Task.FromResult<IEnumerable<FailedPartition>>([]);

    /// <inheritdoc/>
    public Task BeginReplay(ReplayContext context)
    {
        lock (_collectionLock)
        {
            // A replay starts from nothing, so anything a previous replay left behind must go. Keeping it
            // would let a second replay finish holding documents the first one produced.
            _rewindCollection.Clear();
            _rewindLastHandledEventSequenceNumbers.Clear();
            _isReplaying = true;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task ResumeReplay(ReplayContext context)
    {
        // Resuming continues an interrupted replay, so what it has already written is kept.
        lock (_collectionLock)
        {
            _isReplaying = true;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<FailedPartition>> EndReplay(ReplayContext context)
    {
        bool promoted;
        lock (_collectionLock)
        {
            // The replay wrote to the rewind collection, so ending it has to promote that collection -
            // without this the sink kept serving the pre-replay documents and the entire replay result
            // was discarded. The persistent sinks swap the replay container in at this point.
            //
            // A replay that produced no writes is a no-op, not an instruction to empty the read model:
            // promoting an empty collection would turn a transient race - the job observing no keys
            // before the event index caught up - into permanent data loss, which is why MongoDB guards
            // its rename the same way.
            promoted = _rewindCollection.Count > 0;
            if (promoted)
            {
                _collection.Clear();
                _lastHandledEventSequenceNumbers.Clear();

                foreach (var (key, value) in _rewindCollection)
                {
                    _collection[key] = value;
                }

                foreach (var (key, value) in _rewindLastHandledEventSequenceNumbers)
                {
                    _lastHandledEventSequenceNumbers[key] = value;
                }
            }

            _rewindCollection.Clear();
            _rewindLastHandledEventSequenceNumbers.Clear();
            _isReplaying = false;
        }

        // Observers read the primary collection, which has only now taken on what the replay produced.
        if (promoted)
        {
            _changeSubject.OnNext(readModel.ContainerName);
        }

        return Task.FromResult<IEnumerable<FailedPartition>>([]);
    }

    /// <inheritdoc/>
    public Task LeaveReplay()
    {
        // The replay has been promoted somewhere else, so what this sink wrote for it is not the result anyone reads.
        lock (_collectionLock)
        {
            _rewindCollection.Clear();
            _rewindLastHandledEventSequenceNumbers.Clear();
            _isReplaying = false;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Remove(ReadModelContainerName containerName)
    {
        var clearedPrimary = containerName == readModel.ContainerName;
        lock (_collectionLock)
        {
            // The persistent sinks drop the container they are handed, so which one is named decides what
            // goes. Ignoring the name and always clearing the replay state meant removing the container the
            // read model lives in left it readable, and removing a revert container could wipe a replay in
            // flight - the opposite of what was asked for in both cases.
            if (clearedPrimary)
            {
                _collection.Clear();
                _lastHandledEventSequenceNumbers.Clear();
            }
            else
            {
                _rewindCollection.Clear();
                _rewindLastHandledEventSequenceNumbers.Clear();
            }
        }

        // Observers read the primary collection, which has only now been emptied.
        if (clearedPrimary)
        {
            _changeSubject.OnNext(readModel.ContainerName);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task PrepareInitialRun()
    {
        bool clearedPrimary;
        lock (_collectionLock)
        {
            // Inside a replay this clears the rewind collection, which no observer reads.
            clearedPrimary = !_isReplaying;
            Collection.Clear();
            LastHandledEventSequenceNumbers.Clear();
        }

        // Observers read the primary collection, which has only now been emptied.
        if (clearedPrimary)
        {
            _changeSubject.OnNext(readModel.ContainerName);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<Option<Key>> TryFindRootKeyByChildValue(PropertyPath childPropertyPath, object childValue)
    {
        var pathSegments = childPropertyPath.Segments.ToArray();
        KeyValuePair<object, ExpandoObject>[] snapshot;
        lock (_collectionLock)
        {
            snapshot = [.. Collection];
        }

        foreach (var (rootKey, document) in snapshot)
        {
            if (TryFindValueInDocument(document, pathSegments, 0, childValue))
            {
                return Task.FromResult(new Option<Key>(new Key(rootKey, ArrayIndexers.NoIndexers)));
            }
        }

        return Task.FromResult(Option<Key>.None());
    }

    /// <inheritdoc/>
    public Task EnsureIndexes() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<ReadModelInstances> GetInstances(ReadModelContainerName? occurrence = null, int skip = 0, int take = 50) =>
        GetInstances(occurrence, skip, take, ReadModelInstanceVisibility.InitializedOnly);

    /// <inheritdoc/>
    public Task<ReadModelInstances> GetInstances(ReadModelContainerName? occurrence, int skip, int take, ReadModelInstanceVisibility visibility)
    {
        var (instances, totalCount) = SnapshotInstances(skip, take, visibility);
        return Task.FromResult(new ReadModelInstances(instances, totalCount));
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<ExpandoObject>> ObserveInstances(ReadModelContainerName? occurrence = null, int skip = 0, int take = 50) =>
        ObserveInstances(occurrence, skip, take, ReadModelInstanceVisibility.InitializedOnly);

    /// <inheritdoc/>
    public IObservable<IEnumerable<ExpandoObject>> ObserveInstances(ReadModelContainerName? occurrence, int skip, int take, ReadModelInstanceVisibility visibility)
    {
        // For in-memory sink, return an observable that emits whenever the collection changes
        return Observable.Create<IEnumerable<ExpandoObject>>(observer =>
        {
            // Emit initial state
            observer.OnNext(SnapshotInstances(skip, take, visibility).Instances);

            // Subscribe to changes on _changeSubject and emit updated instances
            return _changeSubject.Subscribe(
                _ => observer.OnNext(SnapshotInstances(skip, take, visibility).Instances),
                observer.OnError,
                observer.OnCompleted);
        });
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _changeSubject?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Tell observers that the collection they read has changed.
    /// </summary>
    /// <param name="key">The key of the changed read model.</param>
    /// <param name="wrotePrimary">Whether the write went to the primary collection, decided in the same lock as the write.</param>
    /// <remarks>
    /// A write made while a replay is in progress goes to the replay's own collection, which observers do not read,
    /// so it is not announced; the replay announces its result once it ends and promotes it.
    /// </remarks>
    void NotifyChanged(object key, bool wrotePrimary)
    {
        if (wrotePrimary)
        {
            _changeSubject.OnNext(key);
        }
    }

    (IReadOnlyList<ExpandoObject> Instances, int TotalCount) SnapshotInstances(int skip, int take, ReadModelInstanceVisibility visibility)
    {
        static string KeyOrderValue(object key)
        {
            var value = key switch
            {
                ExpandoObject composite => JsonSerializer.Serialize(composite
                    .OrderBy(entry => entry.Key, StringComparer.Ordinal).ToDictionary(entry => entry.Key, entry => entry.Value)),
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => Convert.ToString(key, CultureInfo.InvariantCulture) ?? string.Empty
            };
            return $"{key.GetType().FullName}:{value}";
        }

        // Always the primary collection, even while a replay is in progress: the replay rebuilds its own
        // collection, and the primary one keeps the previous state until the replay ends and promotes it.
        lock (_collectionLock)
        {
            var visible = _collection.Where(entry =>
                visibility == ReadModelInstanceVisibility.IncludeUninitialized ||
                !((IDictionary<string, object?>)entry.Value).TryGetValue(WellKnownProperties.ReadModelInstanceInitialized, out var initialized) || initialized is not false).ToArray();
            var instances = visible.OrderBy(entry => KeyOrderValue(entry.Key), StringComparer.Ordinal)
                .Skip(skip).Take(take).Select(entry => entry.Value).ToArray();
            return (instances, visible.Length);
        }
    }

    /// <summary>
    /// Determines whether a guarded write would move the read model's last handled event sequence number forward.
    /// </summary>
    /// <param name="keyValue">The resolved read model key.</param>
    /// <param name="eventSequenceNumber">The <see cref="EventSequenceNumber"/> about to be applied.</param>
    /// <returns>True when the write must be applied, false when it must be skipped.</returns>
    /// <remarks>
    /// Callers must hold <c language="csharp">_collectionLock</c>. A guarded write never creates the read model, matching the
    /// persistent sinks, so an absent instance is a skip rather than an insert.
    /// </remarks>
    bool AdvancesWatermark(object keyValue, EventSequenceNumber eventSequenceNumber)
    {
        if (!Collection.ContainsKey(keyValue))
        {
            return false;
        }

        return !LastHandledEventSequenceNumbers.TryGetValue(keyValue, out var lastHandled) ||
               lastHandled < eventSequenceNumber.Value;
    }

    void AdvanceWatermark(object keyValue, EventSequenceNumber eventSequenceNumber)
    {
        // A sentinel is not a position in the sequence; storing one would pin the watermark at the top of the
        // range and permanently block every later guarded write to this instance.
        if (eventSequenceNumber.IsActualValue)
        {
            LastHandledEventSequenceNumbers[keyValue] =
                LastHandledEventSequenceNumbers.TryGetValue(keyValue, out var current)
                    ? Math.Max(current, eventSequenceNumber.Value)
                    : eventSequenceNumber.Value;
        }
    }

    bool TryFindValueInDocument(ExpandoObject document, IPropertyPathSegment[] pathSegments, int segmentIndex, object targetValue)
    {
        if (segmentIndex >= pathSegments.Length)
        {
            return false;
        }

        var currentSegment = pathSegments[segmentIndex];
        var dict = (IDictionary<string, object?>)document;

        if (!dict.TryGetValue(currentSegment.Value, out var value) || value is null)
        {
            return false;
        }

        if (segmentIndex == pathSegments.Length - 1)
        {
            return ValuesAreEqual(value, targetValue);
        }

        if (value is IEnumerable<object> collection)
        {
            foreach (var item in collection)
            {
                if (item is ExpandoObject itemExpando &&
                    TryFindValueInDocument(itemExpando, pathSegments, segmentIndex + 1, targetValue))
                {
                    return true;
                }
            }
        }
        else if (value is ExpandoObject nestedExpando)
        {
            return TryFindValueInDocument(nestedExpando, pathSegments, segmentIndex + 1, targetValue);
        }

        return false;
    }

#pragma warning disable SA1204 // Static elements should appear before instance elements
    static bool ValuesAreEqual(object value, object targetValue)
#pragma warning restore SA1204
    {
        if (value.Equals(targetValue))
        {
            return true;
        }

        var valueString = value.ToString();
        var targetString = targetValue.ToString();

        return valueString == targetString;
    }
}
