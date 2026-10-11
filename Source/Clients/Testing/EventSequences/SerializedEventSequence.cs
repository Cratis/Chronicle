// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;

using System.Reflection;
using System.Runtime.ExceptionServices;
using KernelSequences = KernelCore::Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Serializes asynchronous grain calls, replacing Orleans turn scheduling in the in-process harness.
/// </summary>
internal class SerializedEventSequence : DispatchProxy, IDisposable
{
    readonly SemaphoreSlim _turn = new(1, 1);
    KernelSequences::IEventSequence _grain = null!;

    /// <inheritdoc/>
    public void Dispose()
    {
        _turn.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Creates a serialized grain proxy.
    /// </summary>
    /// <param name="grain">The kernel grain.</param>
    /// <returns>The serialized sequence.</returns>
    internal static KernelSequences::IEventSequence Create(KernelSequences::IEventSequence grain)
    {
        var proxy = Create<KernelSequences::IEventSequence, SerializedEventSequence>();
        ((SerializedEventSequence)proxy)._grain = grain;
        return proxy;
    }

    /// <inheritdoc/>
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        var method = targetMethod!;
        if (method.ReturnType == typeof(Task)) return Run(method, args);
        return typeof(SerializedEventSequence).GetMethod(nameof(RunWithResult), BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(method.ReturnType.GetGenericArguments()[0]).Invoke(this, [method, args]);
    }

    async Task Run(MethodInfo method, object?[]? args)
    {
        await _turn.WaitAsync();
        try
        {
            await (Task)Call(method, args);
        }
        finally
        {
            _turn.Release();
        }
    }

    async Task<T> RunWithResult<T>(MethodInfo method, object?[]? args)
    {
        await _turn.WaitAsync();
        try
        {
            return await (Task<T>)Call(method, args);
        }
        finally
        {
            _turn.Release();
        }
    }

    object Call(MethodInfo method, object?[]? args)
    {
        try
        {
            return method.Invoke(_grain, args)!;
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }
}
