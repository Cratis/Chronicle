// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// A present or absent read model together with its decision guard.
/// </summary>
/// <typeparam name="T">The model type.</typeparam>
public sealed class DecisionRead<T> : IDecisionRead
    where T : class
{
    readonly ConcurrencyScope _scope = null!;

    /// <summary>Creates a guarded read from a fresh session.</summary>
    /// <param name="key">The source key.</param>
    /// <param name="instance">The folded model.</param>
    /// <param name="store">The event store name.</param>
    /// <param name="namespace">The event store namespace.</param>
    /// <param name="boundary">The pre-fold log boundary.</param>
    /// <param name="types">The model's event types.</param>
    internal DecisionRead(ReadModelKey key, T? instance, EventStoreName store, EventStoreNamespaceName @namespace, EventSequenceNumber boundary, EventType[] types)
    {
        Key = key;
        Instance = instance;
        EventStore = store;
        Namespace = @namespace;
        _scope = new ConcurrencyScope(boundary, key, EventTypes: types);
        IsProtected = true;
    }

    DecisionRead(ReadModelKey key, T? instance)
    {
        Key = key;
        Instance = instance;
    }

    /// <inheritdoc/>
    public ReadModelKey Key { get; }

    /// <summary>Gets the instance, or null when absent.</summary>
    public T? Instance { get; }

    /// <summary>Gets whether an instance existed.</summary>
    public bool Exists => Instance is not null;

    /// <inheritdoc/>
    public bool IsProtected { get; }

    /// <inheritdoc/>
    public Type ReadModelType => typeof(T);

    /// <inheritdoc/>
    public EventStoreName EventStore { get; } = EventStoreName.NotSet;

    /// <inheritdoc/>
    public EventStoreNamespaceName Namespace { get; } = EventStoreNamespaceName.NotSet;

    /// <inheritdoc/>
    public EventSequenceId EventSequenceId => EventSequenceId.Log;

    /// <inheritdoc/>
    ConcurrencyScope IDecisionRead.Scope => _scope;

    /// <summary>Wraps a legacy, unguarded read. It cannot be enrolled.</summary>
    /// <param name="key">The model key.</param>
    /// <param name="instance">The instance, if any.</param>
    /// <returns>An unprotected read.</returns>
    public static DecisionRead<T> Unprotected(ReadModelKey key, T? instance) => new(key, instance);
}
