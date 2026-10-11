// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using System.Reactive.Linq;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Monads;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an <see cref="ISink"/> over the read models a projection builds in memory when it processes events
/// without persisting them, such as for a preview.
/// </summary>
/// <param name="identifierProperty">The identifier property from the root read model schema.</param>
/// <remarks>
/// Key resolution asks the sink where an event belongs: which root holds a child that a join event refers to,
/// and whether a keyed update targets a child that already exists. A saved projection answers from what it has
/// persisted. Processing in memory has to answer from the read models it has built so far, or a join event
/// becomes a read model of its own and an update to an existing child cannot find it (Cratis/Chronicle#4727).
/// The sink never writes: the caller decides what to keep through <see cref="Set"/> and <see cref="Remove(string)"/>.
/// </remarks>
internal sealed class PreviewSink(PropertyPath identifierProperty) : ISink
{
    readonly Dictionary<string, PreviewReadModel> _readModels = [];

    /// <inheritdoc/>
    public SinkTypeId TypeId => SinkTypeId.None;

    /// <summary>
    /// Gets the read models built, in the order they were first created.
    /// </summary>
    public IEnumerable<PreviewReadModel> ReadModels => _readModels.Values;

    /// <summary>
    /// Try to get the state of the read model for a key value.
    /// </summary>
    /// <param name="keyValue">The key value of the read model.</param>
    /// <param name="state">The state, when the read model exists.</param>
    /// <returns>True when the read model exists, false if not.</returns>
    public bool TryGet(string keyValue, out ExpandoObject state)
    {
        if (_readModels.TryGetValue(keyValue, out var readModel))
        {
            state = readModel.State;
            return true;
        }

        state = new ExpandoObject();
        return false;
    }

    /// <summary>
    /// Gets initial state with the identifier available for resolving joins that happened before the root.
    /// </summary>
    /// <param name="key">The root key.</param>
    /// <returns>The existing state or a new state carrying its identifier.</returns>
    public ExpandoObject GetInitialState(Key key)
    {
        TryGet(key.Value.ToString()!, out var state);
        if (identifierProperty.IsSet)
        {
            ((IDictionary<string, object?>)state)[identifierProperty.Path] = key.Value;
        }

        return state;
    }

    /// <summary>
    /// Set the state of the read model for a key value, creating it when it does not exist.
    /// </summary>
    /// <param name="keyValue">The key value of the read model.</param>
    /// <param name="state">The state of the read model.</param>
    /// <param name="lastHandledEventSequenceNumber">The sequence number of the last event that changed the read model.</param>
    public void Set(string keyValue, ExpandoObject state, EventSequenceNumber lastHandledEventSequenceNumber) =>
        _readModels[keyValue] = new(keyValue, state, lastHandledEventSequenceNumber);

    /// <summary>
    /// Remove the read model for a key value.
    /// </summary>
    /// <param name="keyValue">The key value of the read model.</param>
    public void Remove(string keyValue) => _readModels.Remove(keyValue);

    /// <summary>
    /// Applies the joins on children that an event's changes carry to every read model holding the child.
    /// </summary>
    /// <param name="changes">The changes the event produced.</param>
    /// <param name="event">The event.</param>
    /// <returns>The joins applied, which are not changes to the read model the event resolved to.</returns>
    /// <remarks>
    /// A join on a child comes from the joined event source, so it is not about the read model its key resolved to.
    /// A saved projection updates every read model holding the child, matching the child by its identifier in the
    /// type the read model stores it as, and so does this. A join on a child the same changes add belongs to that
    /// new child and is left in place, as the sinks do.
    /// </remarks>
    public Joined[] ApplyJoinsOnChildren(IEnumerable<Change> changes, AppendedEvent @event)
    {
        var allChanges = changes.ToArray();
        var joinsOnChildren = allChanges
            .OfType<Joined>()
            .Where(joined => !joined.ArrayIndexers.IsEmpty && !AddsTheJoinedChild(allChanges, joined))
            .ToArray();

        foreach (var joined in joinsOnChildren)
        {
            var child = joined.ArrayIndexers.All.Last();
            foreach (var (readModel, storedIdentifier) in FindHolding(child.ArrayProperty + child.IdentifierProperty, child.Identifier))
            {
                var rebound = RebindChild(joined, child, storedIdentifier);
                ApplyJoin(readModel, rebound, @event);
            }
        }

        return joinsOnChildren;
    }

    /// <summary>
    /// Applies root joins to every read model matching the join property, as the saved sink's UpdateMany does.
    /// </summary>
    /// <param name="changes">The changes the event produced.</param>
    /// <param name="event">The event.</param>
    /// <returns>The joins applied independently of the event's resolved root key.</returns>
    public Joined[] ApplyJoinsOnRoots(IEnumerable<Change> changes, AppendedEvent @event)
    {
        var joins = changes.OfType<Joined>().Where(joined => joined.ArrayIndexers.IsEmpty).ToArray();
        foreach (var joined in joins)
        {
            foreach (var (readModel, _) in FindHolding(joined.OnProperty, joined.Key))
            {
                ApplyJoin(readModel, joined, @event);
            }
        }

        return joins;
    }

    /// <inheritdoc/>
    public Task<ExpandoObject?> FindOrDefault(Key key) =>
        Task.FromResult(_readModels.TryGetValue(key.Value.ToString()!, out var readModel) ? readModel.State : null);

    /// <inheritdoc/>
    public Task<Option<Key>> TryFindRootKeyByChildValue(PropertyPath childPropertyPath, object childValue)
    {
        var holding = FindHolding(childPropertyPath, childValue).Select(_ => _.ReadModel).FirstOrDefault();
        return Task.FromResult(holding is null
            ? Option<Key>.None()
            : new Option<Key>(new Key(holding.KeyValue, ArrayIndexers.NoIndexers)));
    }

    /// <inheritdoc/>
    public Task<IEnumerable<FailedPartition>> ApplyChanges(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset, EventSequenceNumber eventSequenceNumber) =>
        Task.FromResult<IEnumerable<FailedPartition>>([]);

    /// <inheritdoc/>
    public Task BeginBulk() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<IEnumerable<FailedPartition>> EndBulk() => Task.FromResult<IEnumerable<FailedPartition>>([]);

    /// <inheritdoc/>
    public Task BeginReplay(ReplayContext context) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task ResumeReplay(ReplayContext context) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<IEnumerable<FailedPartition>> EndReplay(ReplayContext context) => Task.FromResult<IEnumerable<FailedPartition>>([]);

    /// <inheritdoc/>
    public Task Remove(ReadModelContainerName containerName) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task PrepareInitialRun() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task EnsureIndexes() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<ReadModelInstances> GetInstances(ReadModelContainerName? occurrence = null, int skip = 0, int take = 50) =>
        Task.FromResult(new ReadModelInstances([.. _readModels.Values.Select(_ => _.State).Skip(skip).Take(take)], _readModels.Count));

    /// <inheritdoc/>
    public IObservable<IEnumerable<ExpandoObject>> ObserveInstances(ReadModelContainerName? occurrence = null, int skip = 0, int take = 50) =>
        Observable.Empty<IEnumerable<ExpandoObject>>();

    static bool AddsTheJoinedChild(IEnumerable<Change> changes, Joined joined)
    {
        var child = joined.ArrayIndexers.All.Last();
        return changes.OfType<ChildAdded>().Any(added =>
            added.ChildrenProperty == child.ArrayProperty &&
            added.IdentifiedByProperty == child.IdentifierProperty &&
            added.Key.ToString() == joined.Key.ToString());
    }

    /// <summary>
    /// Rebinds a join to the identifier of the child as the read model stores it.
    /// </summary>
    /// <remarks>
    /// The identifier of a child join is the joined event source id, a string, while the read model may store the
    /// identifier as another type, such as a <see cref="Guid"/>. Children are matched by equality, so the join would
    /// otherwise miss the child it is for and add a new one. The persistent sinks convert it through the schema.
    /// </remarks>
    /// <param name="joined">The join to rebind.</param>
    /// <param name="child">The <see cref="ArrayIndexer"/> of the child the join is for.</param>
    /// <param name="storedIdentifier">The identifier of the child as the read model stores it.</param>
    /// <returns>The join bound to the stored identifier.</returns>
    static Joined RebindChild(Joined joined, ArrayIndexer child, object storedIdentifier)
    {
        var rebound = child with { Identifier = storedIdentifier };
        ArrayIndexers Rebind(ArrayIndexers arrayIndexers) =>
            new(arrayIndexers.All.Select(indexer => IsSameChild(indexer, child) ? rebound : indexer));

        var changes = joined.Changes
            .Select(change => change is PropertiesChanged<ExpandoObject> propertiesChanged
                ? new PropertiesChanged<ExpandoObject>(
                    propertiesChanged.State,
                    [.. propertiesChanged.Differences.Select(difference => new PropertyDifference(difference.PropertyPath, difference.Original, difference.Changed, Rebind(difference.ArrayIndexers)))])
                : change)
            .ToArray();

        return joined with { ArrayIndexers = Rebind(joined.ArrayIndexers), Changes = changes };
    }

    static bool IsSameChild(ArrayIndexer indexer, ArrayIndexer child) =>
        indexer.ArrayProperty == child.ArrayProperty &&
        indexer.IdentifierProperty == child.IdentifierProperty &&
        HasSameValue(indexer.Identifier, child.Identifier);

    static bool HasSameValue(object? value, object other) =>
        value is not null && string.Equals(value.ToString(), other.ToString(), StringComparison.Ordinal);

    static IEnumerable<object> FindValues(object? current, string[] segments, int index, object value)
    {
        if (index == segments.Length)
        {
            return HasSameValue(current, value) ? [current!] : [];
        }

        return current switch
        {
            IDictionary<string, object?> dictionary when dictionary.TryGetValue(segments[index], out var next) => FindValues(next, segments, index + 1, value),
            IEnumerable items and not string and not IDictionary<string, object?> => items.Cast<object?>().SelectMany(item => FindValues(item, segments, index, value)),
            _ => []
        };
    }

    void ApplyJoin(PreviewReadModel readModel, Joined joined, AppendedEvent @event)
    {
        var state = InMemoryChanges.Apply(new Key(readModel.KeyValue, joined.ArrayIndexers), joined.Changes, readModel.State);
        ReadModelSubjects.TrackChanges(state, joined.Changes, @event);
        Set(readModel.KeyValue, state, @event.Context.SequenceNumber);
    }

    IEnumerable<(PreviewReadModel ReadModel, object StoredValue)> FindHolding(PropertyPath propertyPath, object value)
    {
        // The sink persists the identifier from the root key, not from an explicitly mapped property.
        if (propertyPath == identifierProperty)
        {
            return [.. _readModels.Values
                .Where(readModel => HasSameValue(readModel.KeyValue, value))
                .Select(readModel => (readModel, (object)readModel.KeyValue))];
        }

        var segments = propertyPath.Segments.Select(_ => _.Value).ToArray();
        return [.. _readModels.Values
            .Select(readModel => (ReadModel: readModel, StoredValue: FindValues(readModel.State, segments, 0, value).FirstOrDefault()))
            .Where(_ => _.StoredValue is not null)
            .Select(_ => (_.ReadModel, _.StoredValue!))];
    }
}
