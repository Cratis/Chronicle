// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Castle.DynamicProxy;
using MongoDB.Driver;
using Polly;

namespace Cratis.MongoDB;

/// <summary>
/// Represents an interceptor for <see cref="IMongoCollection{TDocument}"/> for methods that returns a <see cref="Task{T}"/>.
/// </summary>
public class MongoCollectionInterceptorForReturnValues : IInterceptor
{
    readonly ResiliencePipeline _resiliencePipeline;
    readonly SemaphoreSlim _openConnectionSemaphore;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoCollectionInterceptorForReturnValues"/> class.
    /// </summary>
    /// <param name="resiliencePipeline">The <see cref="ResiliencePipeline"/> to use.</param>
    /// <param name="openConnectionSemaphore">The <see cref="SemaphoreSlim"/> for keeping track of open connections.</param>
    public MongoCollectionInterceptorForReturnValues(
        ResiliencePipeline resiliencePipeline,
        SemaphoreSlim openConnectionSemaphore)
    {
        _resiliencePipeline = resiliencePipeline;
        _openConnectionSemaphore = openConnectionSemaphore;
    }

    /// <inheritdoc/>
    public void Intercept(IInvocation invocation)
    {
        Task returnTask = null!;
        MethodInfo setResultMethod = null!;
        MethodInfo setExceptionMethod = null!;
        MethodInfo setCanceledMethod = null!;

        var returnType = invocation.Method.ReturnType.GetGenericArguments()[0];
        var taskType = typeof(TaskCompletionSource<>).MakeGenericType(returnType);
        var tcs = Activator.CreateInstance(taskType, new[] { TaskCreationOptions.RunContinuationsAsynchronously })!;
        var tcsType = tcs.GetType();
        setResultMethod = tcsType.GetMethod(nameof(TaskCompletionSource<object>.SetResult))!;
        setExceptionMethod = tcsType.GetMethod(nameof(TaskCompletionSource<object>.SetException), new Type[] { typeof(Exception) })!;
        setCanceledMethod = tcsType.GetMethod(nameof(TaskCompletionSource<object>.SetCanceled), Array.Empty<Type>())!;
        returnTask = (tcsType.GetProperty(nameof(TaskCompletionSource<object>.Task))!.GetValue(tcs) as Task)!;

        invocation.ReturnValue = returnTask!;

#pragma warning disable CA2012 // Use ValueTasks correctly
        _resiliencePipeline.ExecuteAsync(async (_) =>
        {
            await _openConnectionSemaphore.WaitAsync(1000);
            try
            {
                var result = (invocation.Method.Invoke(invocation.InvocationTarget, invocation.Arguments) as Task)!;
                await result.ConfigureAwait(false);

                _openConnectionSemaphore.Release(1);
                if (result.IsCanceled)
                {
                    setCanceledMethod.Invoke(tcs, Array.Empty<object>());
                }
                else
                {
#pragma warning disable CA1849 // Synchronous blocks
                    var taskResult = result.GetType().GetProperty(nameof(Task<object>.Result))!.GetValue(result);
                    setResultMethod.Invoke(tcs, new[] { taskResult });
#pragma warning restore CA1849 // Synchronous blocks
                }
            }
            catch (TaskCanceledException)
            {
                _openConnectionSemaphore.Release(1);
                setCanceledMethod.Invoke(tcs, Array.Empty<object>());
            }
            catch (Exception ex)
            {
                _openConnectionSemaphore.Release(1);
                setExceptionMethod.Invoke(tcs, new[] { ex });
            }

            return ValueTask.CompletedTask;
        });
#pragma warning restore CA2012 // Use ValueTasks correctly
    }
}
