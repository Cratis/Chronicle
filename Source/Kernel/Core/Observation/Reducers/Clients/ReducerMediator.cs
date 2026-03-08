// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Dynamic;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Observation.Reducers.Clients;

/// <summary>
/// Represents an implementation of <see cref="IReducerMediator"/>.
/// </summary>
[Singleton]
public class ReducerMediator : IReducerMediator
{
    readonly ConcurrentDictionary<ReducerMediatorKey, ReducerEventsObserver> _observers = new();

    /// <inheritdoc/>
    public void Subscribe(
        ReducerId reducerId,
        ConnectionId connectionId,
        ReducerEventsObserver target)
    {
        _observers[new(reducerId, connectionId)] = target;
    }

    /// <inheritdoc/>
    public void OnNext(
        ReducerId reducerId,
        ConnectionId connectionId,
        ReduceOperation operation,
        TaskCompletionSource<ReducerSubscriberResult> taskCompletionSource)
    {
        if (_observers.TryGetValue(new(reducerId, connectionId), out var observable))
        {
            observable(operation, taskCompletionSource);
        }
        else
        {
            taskCompletionSource.SetResult(new(ObserverSubscriberResult.Disconnected(), new ExpandoObject()));
        }
    }

    /// <inheritdoc/>
    public void Disconnected(
        ReducerId reducerId,
        ConnectionId connectionId)
    {
        _observers.TryRemove(new(reducerId, connectionId), out var _);
    }
}
