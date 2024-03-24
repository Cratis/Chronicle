// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Castle.DynamicProxy;
using MongoDB.Driver;
using Polly;

namespace Cratis.MongoDB;

/// <summary>
/// Represents an interceptor for <see cref="IMongoClient"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MongoDatabaseInterceptor"/> class.
/// </remarks>
/// <param name="proxyGenerator"><see cref="ProxyGenerator"/> for creating further proxies.</param>
/// <param name="resiliencePipeline">The <see cref="ResiliencePipeline"/> to use.</param>
/// <param name="mongoClient"><see cref="IMongoClient"/> the interceptor is for.</param>
public class MongoDatabaseInterceptor(ProxyGenerator proxyGenerator, ResiliencePipeline resiliencePipeline, IMongoClient mongoClient) : IInterceptor
{
    /// <inheritdoc/>
    public void Intercept(IInvocation invocation)
    {
        invocation.Proceed();

        invocation.ReturnValue = proxyGenerator.CreateInterfaceProxyWithTarget(
            invocation.Method.ReturnType,
            invocation.ReturnValue,
            new ProxyGenerationOptions
            {
                Selector = new MongoCollectionInterceptorSelector(resiliencePipeline, mongoClient)
            });
    }
}
