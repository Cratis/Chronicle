// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Castle.DynamicProxy;
using MongoDB.Driver;
using Polly;

namespace Cratis.MongoDB;

/// <summary>
/// Represents an interceptor for <see cref="IMongoCollection{TDocument}"/>.
/// </summary>
public class MongoCollectionInterceptor : IInterceptor
{
    readonly ResiliencePipeline _resiliencePipeline;
    readonly SemaphoreSlim _openConnectionSemaphore;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoCollectionInterceptorForReturnValues"/> class.
    /// </summary>
    /// <param name="resiliencePipeline">The <see cref="ResiliencePipeline"/> to use.</param>
    /// <param name="openConnectionSemaphore">The <see cref="SemaphoreSlim"/> for keeping track of open connections.</param>
    public MongoCollectionInterceptor(
        ResiliencePipeline resiliencePipeline,
        SemaphoreSlim openConnectionSemaphore)
    {
        _resiliencePipeline = resiliencePipeline;
        _openConnectionSemaphore = openConnectionSemaphore;
    }

    /// <inheritdoc/>
    public void Intercept(IInvocation invocation)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        invocation.ReturnValue = tcs.Task;

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
                    tcs.SetCanceled();
                }
                else
                {
                    tcs.SetResult();
                }
            }
            catch (TaskCanceledException)
            {
                _openConnectionSemaphore.Release(1);
                tcs.SetCanceled();
            }
            catch (Exception ex)
            {
                _openConnectionSemaphore.Release(1);
                tcs.SetException(ex);
                return ValueTask.FromException(ex);
            }

            return ValueTask.CompletedTask;
        });
#pragma warning restore CA2012 // Use ValueTasks correctly
    }
}
