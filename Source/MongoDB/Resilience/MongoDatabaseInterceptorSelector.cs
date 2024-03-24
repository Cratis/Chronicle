// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Castle.DynamicProxy;
using MongoDB.Driver;
using Polly;

namespace Cratis.MongoDB;

/// <summary>
/// Represents a selector for <see cref="MongoDatabaseInterceptor"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MongoDatabaseInterceptorSelector"/> class.
/// </remarks>
/// <param name="proxyGenerator"><see cref="ProxyGenerator"/> for creating further proxies.</param>
/// <param name="resiliencePipeline">The <see cref="ResiliencePipeline"/> to use.</param>
/// <param name="mongoClient"><see cref="IMongoClient"/> to intercept.</param>
public class MongoDatabaseInterceptorSelector(
    ProxyGenerator proxyGenerator,
    ResiliencePipeline resiliencePipeline,
    IMongoClient mongoClient) : IInterceptorSelector
{
    /// <inheritdoc/>
    public IInterceptor[] SelectInterceptors(Type type, MethodInfo method, IInterceptor[] interceptors)
    {
        if (method.Name == nameof(IMongoDatabase.GetCollection))
        {
            return new[] { new MongoDatabaseInterceptor(proxyGenerator, resiliencePipeline, mongoClient) };
        }
        return [];
    }
}
