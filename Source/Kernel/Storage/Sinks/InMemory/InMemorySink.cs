// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text;
using Cratis.Changes;
using Cratis.Dynamic;
using Cratis.Events;
using Cratis.Json;
using Cratis.Kernel.Keys;
using Cratis.Models;
using Cratis.Reflection;
using Cratis.Schemas;
using Cratis.Sinks;
using Cratis.Types;

namespace Cratis.Kernel.Storage.Sinks.InMemory;

/// <summary>
/// Represents an implementation of <see cref="ISink"/> for working with projections in memory.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="InMemorySink"/> class.
/// </remarks>
/// <param name="model">The target <see cref="Model"/>.</param>
/// <param name="typeFormats">The <see cref="ITypeFormats"/> for resolving actual types from JSON schema.</param>
public class InMemorySink(
    Model model,
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
    /// Remove any existing model by the given key.
    /// </summary>
    /// <param name="key"><see cref="Key"/> for the model to remove.</param>
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
    public Task ApplyChanges(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset)
    {
        var state = changeset.InitialState.Clone();
        var collection = Collection;
        var keyValue = GetActualKeyValue(key);

        if (changeset.HasBeenRemoved())
        {
            collection.Remove(keyValue);
            return Task.CompletedTask;
        }

        var result = ApplyActualChanges(key, changeset.Changes, state);
        ((dynamic)result).id = key.Value;
        collection[keyValue] = result;

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task BeginReplay()
    {
        _isReplaying = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task EndReplay()
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

        var targetType = model.Schema.GetTargetTypeForPropertyPath("id", typeFormats);
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
}
