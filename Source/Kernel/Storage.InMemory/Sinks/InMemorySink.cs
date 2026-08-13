// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
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
public class InMemorySink(
    ReadModelDefinition readModel,
    ITypeFormats typeFormats) : ISink, IDisposable
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
    /// <remarks>
    /// Two <see cref="Key"/> instances addressing the same document — a concept and its underlying primitive,
    /// say — resolve to the same value here. Callers that keep per-document state beside the sink must derive
    /// it from this rather than from <see cref="Key.Value"/>, or the two disagree on what "the same document" is.
    /// </remarks>
    public object GetKeyValue(Key key)
    {
        if (key.Value is ExpandoObject expandoKey)
        {
            var stringBuilder = new StringBuilder();
            foreach (var (_, value) in expandoKey.GetKeyValuePairs().OrderBy(_ => _.Key))
            {
                if (stringBuilder.Length > 0) stringBuilder.Append('_');
                stringBuilder.Append(value);
            }

            return stringBuilder.ToString();
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
    public Task<IEnumerable<FailedPartition>> ApplyChanges(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset, EventSequenceNumber eventSequenceNumber) =>
        ApplyChanges(key, changeset, eventSequenceNumber, SinkWriteMode.Always);

    /// <inheritdoc/>
    public Task<IEnumerable<FailedPartition>> ApplyChanges(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset, EventSequenceNumber eventSequenceNumber, SinkWriteMode mode)
    {
        var state = changeset.InitialState.Clone();
        var keyValue = GetKeyValue(key);

        if (changeset.HasBeenRemoved())
        {
            lock (_collectionLock)
            {
                Collection.Remove(keyValue);
                LastHandledEventSequenceNumbers.Remove(keyValue);
            }

            _changeSubject.OnNext(keyValue);
            return Task.FromResult<IEnumerable<FailedPartition>>([]);
        }

        var result = ApplyActualChanges(key, changeset.Changes, state);
        ((dynamic)result).id = key.Value;
        lock (_collectionLock)
        {
            if (mode == SinkWriteMode.OnlyWhenAdvancingWatermark &&
                eventSequenceNumber.IsActualValue &&
                !AdvancesWatermark(keyValue, eventSequenceNumber))
            {
                return Task.FromResult<IEnumerable<FailedPartition>>([]);
            }

            Collection[keyValue] = result;

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

        // Notify observers of the change
        _changeSubject.OnNext(keyValue);

        return Task.FromResult<IEnumerable<FailedPartition>>([]);
    }

    /// <inheritdoc/>
    public Task BeginBulk() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task EndBulk() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task BeginReplay(ReplayContext context)
    {
        _isReplaying = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task ResumeReplay(ReplayContext context)
    {
        _isReplaying = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task EndReplay(ReplayContext context)
    {
        _isReplaying = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Remove(ReadModelContainerName containerName)
    {
        lock (_collectionLock)
        {
            _rewindCollection.Clear();
            _rewindLastHandledEventSequenceNumbers.Clear();
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task PrepareInitialRun()
    {
        lock (_collectionLock)
        {
            Collection.Clear();
            LastHandledEventSequenceNumbers.Clear();
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
    public Task<ReadModelInstances> GetInstances(ReadModelContainerName? occurrence = null, int skip = 0, int take = 50)
    {
        var (instances, totalCount) = SnapshotInstances(skip, take);
        return Task.FromResult(new ReadModelInstances(instances, totalCount));
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<ExpandoObject>> ObserveInstances(ReadModelContainerName? occurrence = null, int skip = 0, int take = 50)
    {
        // For in-memory sink, return an observable that emits whenever the collection changes
        return Observable.Create<IEnumerable<ExpandoObject>>(observer =>
        {
            // Emit initial state
            observer.OnNext(SnapshotInstances(skip, take).Instances);

            // Subscribe to changes on _changeSubject and emit updated instances
            return _changeSubject.Subscribe(
                _ => observer.OnNext(SnapshotInstances(skip, take).Instances),
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

    (IReadOnlyList<ExpandoObject> Instances, int TotalCount) SnapshotInstances(int skip, int take)
    {
        lock (_collectionLock)
        {
            var collection = Collection;
            var instances = collection.Values.Skip(skip).Take(take).ToArray();
            return (instances, collection.Count);
        }
    }

    /// <summary>
    /// Determines whether a guarded write would move the read model's last handled event sequence number forward.
    /// </summary>
    /// <param name="keyValue">The resolved read model key.</param>
    /// <param name="eventSequenceNumber">The <see cref="EventSequenceNumber"/> about to be applied.</param>
    /// <returns>True when the write must be applied, false when it must be skipped.</returns>
    /// <remarks>
    /// Callers must hold <c>_collectionLock</c>. A guarded write never creates the read model, matching the
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

    ExpandoObject ApplyActualChanges(Key key, IEnumerable<Change> changes, ExpandoObject state)
    {
        var changesToApply = changes.ToList();
        var collectionPathsWithChildOperations = changesToApply.GetCollectionPathsWithChildOperations();
        var wholeCollectionReplacementPaths = changesToApply.GetWholeCollectionReplacementPaths();

        foreach (var change in changesToApply)
        {
            switch (change)
            {
                case PropertiesChanged<ExpandoObject> propertiesChanged:
                    state = propertiesChanged.ApplyToStateWithoutChildOperationConflicts(state, collectionPathsWithChildOperations, wholeCollectionReplacementPaths);
                    break;

                case ChildAdded childAdded:
                    var collection = state.EnsureCollection<ExpandoObject, object>(childAdded.ChildrenProperty, childAdded.ArrayIndexers);
                    collection.Add(childAdded.State);
                    break;

                case ChildRemoved childRemoved:
                    var childCollection = state.EnsureCollection<ExpandoObject, object>(childRemoved.ChildrenProperty, key.ArrayIndexers);
                    var childToRemove = childCollection.FindByKey(childRemoved.IdentifiedByProperty, childRemoved.Key);
                    if (childToRemove is not null)
                    {
                        childCollection.Remove(childToRemove);
                    }

                    break;

                case NestedCleared nestedCleared:
                    var stateDict = (IDictionary<string, object?>)state;
                    stateDict[nestedCleared.NestedProperty.LastSegment.Value] = null;
                    break;

                case Joined joined:
                    state = ApplyActualChanges(key, joined.Changes, state);
                    break;

                case ResolvedJoin resolvedJoin:
                    state = ApplyActualChanges(key, resolvedJoin.Changes, state);
                    break;
            }
        }

        return state;
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
