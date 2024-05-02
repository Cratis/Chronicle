// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Castle.DynamicProxy;
using MongoDB.Driver;
using Polly;

namespace Cratis.MongoDB;

/// <summary>
/// Represents a selector for <see cref="MongoCollectionInterceptorForReturnValues"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MongoCollectionInterceptorSelector"/> class.
/// </remarks>
/// <param name="resiliencePipeline">The <see cref="ResiliencePipeline"/> to use.</param>
/// <param name="mongoClient"><see cref="IMongoClient"/> to intercept.</param>
public class MongoCollectionInterceptorSelector(
    ResiliencePipeline resiliencePipeline,
    IMongoClient mongoClient) : IInterceptorSelector
{
    /// <inheritdoc/>
    public IInterceptor[] SelectInterceptors(Type type, MethodInfo method, IInterceptor[] interceptors)
    {
#pragma warning disable CA2000 // Dispose objects before losing scope.
        var semaphore = new SemaphoreSlim(mongoClient.Settings.MaxConnectionPoolSize / 2, mongoClient.Settings.MaxConnectionPoolSize / 2);
#pragma warning restore CA2000 // Dispose objects before losing scope.
        if (method.ReturnType.IsAssignableTo(typeof(Task)))
        {
            if (method.ReturnType.IsGenericType)
            {
                return new[] { new MongoCollectionInterceptorForReturnValues(resiliencePipeline, semaphore) };
            }

            return new[] { new MongoCollectionInterceptor(resiliencePipeline, semaphore) };
        }
        return [];
    }
}
