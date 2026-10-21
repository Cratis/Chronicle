// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Cratis.Chronicle.Integration.Clustering.for_Occurrences;

/// <summary>
/// Test-only storage decoration: every call goes to real storage, including unknown/failed results.
/// </summary>
public class StorageObservationProxy : DispatchProxy
{
    object _target = null!;
    Func<string, object?, object?> _observe = null!;

    public static T Wrap<T>(T target, Func<string, object?, object?> observe)
        where T : class
    {
        var proxy = Create<T, StorageObservationProxy>();
        var state = (StorageObservationProxy)(object)proxy;
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
