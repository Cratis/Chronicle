// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_UniqueConstraintsStorageV2.given;

public class V2ForwardingProxy<T> : DispatchProxy
    where T : class
{
    public T Target { get; set; } = null!;
    public V2Interleaving Gates { get; set; } = null!;

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        var method = targetMethod!;
        var arguments = args ?? [];
        if (method.ReturnType == typeof(Task)) return InvokeTask(method, arguments);
        if (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            return GetType().GetMethod(nameof(InvokeResult), BindingFlags.Instance | BindingFlags.NonPublic)!
                .MakeGenericMethod(method.ReturnType.GetGenericArguments()[0]).Invoke(this, [method, arguments]);
        }

        return Gates.Wrap(Forward(method, arguments));
    }

    object? Forward(MethodInfo method, object?[] args)
    {
        try
        {
            return method.Invoke(Target, args);
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    async Task InvokeTask(MethodInfo method, object?[] args)
    {
        await Gates.Before(Target, method, args);
        var task = (Task)Forward(method, args)!;
        Gates.Started(Target, method, args);
        await task;
        await Gates.Completed(Target, method, args);
    }

    protected async Task<TResult> InvokeResult<TResult>(MethodInfo method, object?[] args)
    {
        await Gates.Before(Target, method, args);
        var task = (Task<TResult>)Forward(method, args)!;
        Gates.Started(Target, method, args);
        var result = await task;
        await Gates.Completed(Target, method, args);
        return (TResult)Gates.Wrap(result)!;
    }
}
