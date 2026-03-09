// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Identities;
using Cratis.Execution;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.AspNetCore;

/// <summary>
/// Extension methods for configuring structural dependencies on <see cref="IChronicleBuilder"/>.
/// </summary>
public static class ChronicleBuilderExtensions
{
    /// <summary>
    /// Configures the <see cref="IClientArtifactsProvider"/> to use for artifact discovery.
    /// </summary>
    /// <param name="builder"><see cref="IChronicleBuilder"/> to configure.</param>
    /// <param name="provider">The <see cref="IClientArtifactsProvider"/> to use.</param>
    /// <returns>The same <see cref="IChronicleBuilder"/> for continuation.</returns>
    public static IChronicleBuilder WithArtifactsProvider(this IChronicleBuilder builder, IClientArtifactsProvider provider)
    {
        builder.ClientArtifactsProvider = provider;
        return builder;
    }

    /// <summary>
    /// Configures the <see cref="IIdentityProvider"/> to use for resolving identity information.
    /// </summary>
    /// <param name="builder"><see cref="IChronicleBuilder"/> to configure.</param>
    /// <param name="identityProvider">The <see cref="IIdentityProvider"/> to use.</param>
    /// <returns>The same <see cref="IChronicleBuilder"/> for continuation.</returns>
    public static IChronicleBuilder WithIdentityProvider(this IChronicleBuilder builder, IIdentityProvider identityProvider)
    {
        builder.IdentityProvider = identityProvider;
        return builder;
    }

    /// <summary>
    /// Configures the <see cref="ICorrelationIdAccessor"/> to use for accessing the current correlation ID.
    /// </summary>
    /// <param name="builder"><see cref="IChronicleBuilder"/> to configure.</param>
    /// <param name="correlationIdAccessor">The <see cref="ICorrelationIdAccessor"/> to use.</param>
    /// <returns>The same <see cref="IChronicleBuilder"/> for continuation.</returns>
    public static IChronicleBuilder WithCorrelationIdAccessor(this IChronicleBuilder builder, ICorrelationIdAccessor correlationIdAccessor)
    {
        builder.CorrelationIdAccessor = correlationIdAccessor;
        return builder;
    }

    /// <summary>
    /// Configures the <see cref="IEventStoreNamespaceResolver"/> to use for per-request namespace resolution.
    /// </summary>
    /// <param name="builder"><see cref="IChronicleBuilder"/> to configure.</param>
    /// <param name="resolver">The <see cref="IEventStoreNamespaceResolver"/> to use.</param>
    /// <returns>The same <see cref="IChronicleBuilder"/> for continuation.</returns>
    public static IChronicleBuilder WithNamespaceResolver(this IChronicleBuilder builder, IEventStoreNamespaceResolver resolver)
    {
        builder.NamespaceResolver = resolver;
        return builder;
    }

    /// <summary>
    /// Configures the <see cref="ILoggerFactory"/> to use for creating loggers.
    /// </summary>
    /// <param name="builder"><see cref="IChronicleBuilder"/> to configure.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to use.</param>
    /// <returns>The same <see cref="IChronicleBuilder"/> for continuation.</returns>
    public static IChronicleBuilder WithLoggerFactory(this IChronicleBuilder builder, ILoggerFactory loggerFactory)
    {
        builder.LoggerFactory = loggerFactory;
        return builder;
    }
}
