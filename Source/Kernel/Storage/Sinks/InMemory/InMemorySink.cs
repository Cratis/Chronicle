// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Monads;
using Cratis.Reflection;
using Cratis.Types;

namespace Cratis.Chronicle.Storage.Sinks.InMemory;

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
    bool _isReplaying;

    /// <inheritdoc/>
    public SinkTypeId TypeId => WellKnownSinkTypes.InMemory;

    /// <inheritdoc/>
    public SinkTypeName Name => "InMemory";

    /// <summary>
    /// Gets the current collection for the sink represented as a key value of key to <see cref="ExpandoObject"/>.
    /// </summary>
    public IDictionary<object, ExpandoObject> Collection => _isReplaying ? _rewindCollection : _collection;

    /// <summary>
    /// Remove any existing read model by the given key.
    /// </summary>
    /// <param name="key"><see cref="Key"/> for the read model to remove.</param>
    public void RemoveAnyExisting(Key key)
    {
        var collection = Collection;
        var keyValue = GetActualKeyValue(key);
        collection.Remove(keyValue);
    }

    /// <inheritdoc/>
    public Task<ExpandoObject?> FindOrDefault(Key key)
    {
        var collection = Collection;
        var keyValue = GetActualKeyValue(key);
        if (collection.TryGetValue(keyValue, out var value)) return Task.FromResult<ExpandoObject?>(value);

        return Task.FromResult<ExpandoObject?>(null);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<FailedPartition>> ApplyChanges(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset, EventSequenceNumber eventSequenceNumber)
    {
        var state = changeset.InitialState.Clone();
        var collection = Collection;
        var keyValue = GetActualKeyValue(key);

        if (changeset.HasBeenRemoved())
        {
            collection.Remove(keyValue);
            return Task.FromResult<IEnumerable<FailedPartition>>(Array.Empty<FailedPartition>());
        }

        var result = ApplyActualChanges(key, changeset.Changes, state);
        ((dynamic)result).id = key.Value;
        collection[keyValue] = result;

        return Task.FromResult<IEnumerable<FailedPartition>>(Array.Empty<FailedPartition>());
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
    public Task PrepareInitialRun()
    {
        Collection.Clear();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<Option<Key>> TryFindRootKeyByChildValue(PropertyPath childPropertyPath, object childValue)
    {
        var collection = Collection;
        var pathSegments = childPropertyPath.Segments.ToArray();

        foreach (var (rootKey, document) in collection)
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
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    object GetActualKeyValue(Key key)
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

        var targetType = readModel.GetSchemaForLatestGeneration().GetTargetTypeForPropertyPath("id", typeFormats);
        if (targetType is not null)
        {
            return TypeConversion.Convert(targetType, key.Value);
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

    ExpandoObject ApplyActualChanges(Key key, IEnumerable<Change> changes, ExpandoObject state)
    {
        foreach (var change in changes)
        {
            switch (change)
            {
                case PropertiesChanged<ExpandoObject>:
                    state = state.MergeWith((change.State as ExpandoObject)!);
                    break;

                case ChildAdded childAdded:
                    var collection = state.EnsureCollection<ExpandoObject, object>(childAdded.ChildrenProperty, key.ArrayIndexers);
                    collection.Add(childAdded.State);
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
