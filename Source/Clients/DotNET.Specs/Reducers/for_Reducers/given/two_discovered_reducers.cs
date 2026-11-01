// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reducers.for_Reducers.given;

public class two_discovered_reducers : all_dependencies
{
    protected IReducerHandler _first;
    protected IReducerHandler _second;
    protected CancellationToken _firstToken;
    protected CancellationToken _secondToken;

    async Task Establish()
    {
        _clientArtifacts.Reducers.Returns([typeof(First), typeof(Second)]);
        _eventTypes.AllClrTypes.Returns([typeof(Changed)]);
        _eventTypes.GetEventTypeFor(typeof(Changed)).Returns(new EventType("changed", 1));
        _eventStore.Reducers.Returns(_reducers);
        await _reducers.Discover();
        var handlers = (IReadOnlyDictionary<Type, IReducerHandler>)typeof(Reducers)
            .GetField("_handlersByType", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_reducers)!;
        _first = handlers.Values.First();
        _second = handlers.Values.Last();
        _firstToken = _first.CancellationToken;
        _secondToken = _second.CancellationToken;
    }

    void Destroy()
    {
        _reducers.Release();
        (_first as IDisposable)?.Dispose();
        (_second as IDisposable)?.Dispose();
    }

    [EventType] public record Changed;

    public record First
    {
        public static First Reduce(Changed @event, First? current) => current ?? new();
    }

    public record Second
    {
        public static Second Reduce(Changed @event, Second? current) => current ?? new();
    }
}
