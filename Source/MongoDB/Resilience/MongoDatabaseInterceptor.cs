// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Castle.DynamicProxy;
using MongoDB.Driver;
using Polly;

namespace Cratis.MongoDB;

/// <summary>
/// Represents an interceptor for <see cref="IMongoClient"/>.
/// </summary>
public class MongoDatabaseInterceptor : IInterceptor
{
    readonly ProxyGenerator _proxyGenerator;
    readonly ResiliencePipeline _resiliencePipeline;
    readonly IMongoClient _mongoClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDatabaseInterceptor"/> class.
    /// </summary>
    /// <param name="proxyGenerator"><see cref="ProxyGenerator"/> for creating further proxies.</param>
    /// <param name="resiliencePipeline">The <see cref="ResiliencePipeline"/> to use.</param>
    /// <param name="mongoClient"><see cref="IMongoClient"/> the interceptor is for.</param>
    public MongoDatabaseInterceptor(ProxyGenerator proxyGenerator, ResiliencePipeline resiliencePipeline, IMongoClient mongoClient)
    {
        _proxyGenerator = proxyGenerator;
        _resiliencePipeline = resiliencePipeline;
        _mongoClient = mongoClient;
    }

    /// <inheritdoc/>
    public void Intercept(IInvocation invocation)
    {
        invocation.Proceed();

        invocation.ReturnValue = _proxyGenerator.CreateInterfaceProxyWithTarget(
            invocation.Method.ReturnType,
            invocation.ReturnValue,
            new ProxyGenerationOptions
            {
                Selector = new MongoCollectionInterceptorSelector(_resiliencePipeline, _mongoClient)
            });
    }
}
