// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Castle.DynamicProxy;
using MongoDB.Driver;
using Polly;

namespace Cratis.MongoDB;

/// <summary>
/// Represents a selector for <see cref="MongoClientInterceptor"/>.
/// </summary>
public class MongoClientInterceptorSelector : IInterceptorSelector
{
    readonly ProxyGenerator _proxyGenerator;
    readonly ResiliencePipeline _resiliencePipeline;
    readonly IMongoClient _mongoClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoClientInterceptorSelector"/> class.
    /// </summary>
    /// <param name="proxyGenerator"><see cref="ProxyGenerator"/> for creating further proxies.</param>
    /// <param name="resiliencePipeline">The <see cref="ResiliencePipeline"/> to use.</param>
    /// <param name="mongoClient"><see cref="IMongoClient"/> to intercept.</param>
    public MongoClientInterceptorSelector(
        ProxyGenerator proxyGenerator,
        ResiliencePipeline resiliencePipeline,
        IMongoClient mongoClient)
    {
        _proxyGenerator = proxyGenerator;
        _resiliencePipeline = resiliencePipeline;
        _mongoClient = mongoClient;
    }

    /// <inheritdoc/>
    public IInterceptor[] SelectInterceptors(Type type, MethodInfo method, IInterceptor[] interceptors)
    {
        if (method.Name == nameof(IMongoClient.GetDatabase))
        {
            return new[] { new MongoClientInterceptor(_proxyGenerator, _resiliencePipeline, _mongoClient) };
        }
        return Array.Empty<IInterceptor>();
    }
}
