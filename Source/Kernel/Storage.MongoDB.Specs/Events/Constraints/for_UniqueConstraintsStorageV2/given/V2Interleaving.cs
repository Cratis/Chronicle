// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Storage.MongoDB.Events.Constraints.V2;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_UniqueConstraintsStorageV2.given;

public sealed class V2Interleaving
{
    TaskCompletionSource _prechecked = Signal();
    TaskCompletionSource _fenced = Signal();
    TaskCompletionSource _transitionStarted = Signal();
    TaskCompletionSource _release = Signal();
    TaskCompletionSource _both = Signal();
    bool _armed;
    bool _transitionFirst;
    bool _competing;
    int _arrivals;

    public Func<object, MethodInfo, object?[], Task>? BeforeCall { get; set; }
    public Func<object, MethodInfo, object?[], Task>? AfterCall { get; set; }

    public T Forward<T>(T target) where T : class
    {
        var proxy = DispatchProxy.Create<T, V2ForwardingProxy<T>>();
        var forwarding = (V2ForwardingProxy<T>)(object)proxy;
        forwarding.Target = target;
        forwarding.Gates = this;
        return proxy;
    }

    public object? Wrap(object? result) => result switch
    {
        IMongoDatabase database => Forward(database),
        IMongoClient client => Forward(client),
        IMongoCollection<BsonDocument> collection => Forward(collection),
        _ => result
    };

    public async Task Before(object target, MethodInfo method, object?[] args)
    {
        if (BeforeCall is not null) await BeforeCall(target, method, args);
        if (_armed && _transitionFirst) await PausePrechecked(target, method);
        if (_competing && IsTransition(target, method, args))
        {
            if (Interlocked.Increment(ref _arrivals) == 2) _both.TrySetResult();
            await _both.Task.WaitAsync(TimeSpan.FromSeconds(20));
        }
    }

    public void Started(object target, MethodInfo method, object?[] args)
    {
        if (!_armed) return;
        if (IsTransition(target, method, args)) _transitionStarted.TrySetResult();
    }

    public async Task Completed(object target, MethodInfo method, object?[] args)
    {
        if (AfterCall is not null) await AfterCall(target, method, args);
        if (!_armed) return;
        if (_transitionFirst) return;
        if (!IsFencedUpdate(target, method, args)) return;
        _fenced.TrySetResult();
        await _release.Task.WaitAsync(TimeSpan.FromSeconds(20));
    }

    public async Task<T> Run<T>(Func<Task> transition, Func<Task<T>> mutation, bool transitionFirst)
    {
        _prechecked = Signal();
        _fenced = Signal();
        _transitionStarted = Signal();
        _release = Signal();
        _transitionFirst = transitionFirst;
        _armed = true;
        var writer = Task.Run(mutation);
        try
        {
            if (transitionFirst)
            {
                await _prechecked.Task.WaitAsync(TimeSpan.FromSeconds(20));
                await transition();
            }
            else
            {
                await _fenced.Task.WaitAsync(TimeSpan.FromSeconds(20));
                var transitioning = transition();
                await _transitionStarted.Task.WaitAsync(TimeSpan.FromSeconds(20));
                _release.TrySetResult();
                await transitioning;
            }

            _release.TrySetResult();
            return await writer.WaitAsync(TimeSpan.FromSeconds(20));
        }
        finally
        {
            Release();
            await writer.WaitAsync(TimeSpan.FromSeconds(20));
        }
    }

    public async Task<T[]> Concurrent<T>(Func<Task<T>> first, Func<Task<T>> second)
    {
        _both = Signal();
        _arrivals = 0;
        _competing = true;
        try
        {
            return await Task.WhenAll(Task.Run(first), Task.Run(second)).WaitAsync(TimeSpan.FromSeconds(20));
        }
        finally
        {
            Release();
        }
    }

    public void Release()
    {
        _release.TrySetResult();
        _both.TrySetResult();
        _armed = false;
        _competing = false;
    }

    async Task PausePrechecked(object target, MethodInfo method)
    {
        if (target is not IMongoClient) return;
        if (method.Name != nameof(IMongoClient.StartSessionAsync)) return;
        _prechecked.TrySetResult();
        await _release.Task.WaitAsync(TimeSpan.FromSeconds(20));
    }

    static bool IsTransition(object target, MethodInfo method, object?[] args) => IsAuthorityUpdate(target, method) && args[0] is not IClientSessionHandle;

    static bool IsFencedUpdate(object target, MethodInfo method, object?[] args) => IsAuthorityUpdate(target, method) && args[0] is IClientSessionHandle;

    static bool IsAuthorityUpdate(object target, MethodInfo method) => target is IMongoCollection<BsonDocument> collection && collection.CollectionNamespace.CollectionName == V2Schema.Authorities && method.Name == nameof(IMongoCollection<BsonDocument>.UpdateOneAsync);

    static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
