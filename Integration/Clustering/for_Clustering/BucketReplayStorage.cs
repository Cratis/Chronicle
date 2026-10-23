// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.ExceptionServices;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Integration.Clustering.for_Clustering;

public class BucketReplayStorage : DispatchProxy
{
    object _target = null!;
    Func<string, object?, object?> _observe = null!;

    public static IStorage Decorate(IStorage target, ReplayCursorCheckpoint checkpoint) => Wrap(target,
        (name, result) => name == nameof(IStorage.GetEventStore) ? DecorateStore((IEventStoreStorage)result!, checkpoint) : result);

    static IEventStoreStorage DecorateStore(IEventStoreStorage target, ReplayCursorCheckpoint checkpoint) => Wrap(target,
        (name, result) => name == nameof(IEventStoreStorage.GetNamespace) ? DecorateNamespace((IEventStoreNamespaceStorage)result!, checkpoint) : result);

    static IEventStoreNamespaceStorage DecorateNamespace(IEventStoreNamespaceStorage target, ReplayCursorCheckpoint checkpoint) => Wrap(target,
        (name, result) => name == nameof(IEventStoreNamespaceStorage.GetEventSequence) ? Wrap((IEventSequenceStorage)result!, (_, value) => checkpoint.Observe(value)) : result);

    static T Wrap<T>(T target, Func<string, object?, object?> observe)
        where T : class
    {
        var proxy = Create<T, BucketReplayStorage>();
        var state = (BucketReplayStorage)(object)proxy;
        state._target = target;
        state._observe = observe;
        return proxy;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        try
        {
            return _observe(targetMethod!.Name, targetMethod.Invoke(_target, args));
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }
}
