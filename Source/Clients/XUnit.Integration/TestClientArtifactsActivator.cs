// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// A test-specific <see cref="IClientArtifactsActivator"/> decorator that wraps any
/// <see cref="IServiceProvider"/> passed to <c>Activate</c> with a
/// <see cref="FallbackServiceProvider"/> so that types registered in the
/// <see cref="MutableServiceRegistry"/> can be resolved — even though Microsoft DI's
/// built-in <c>IServiceProvider</c> injection bypasses the wrapper.
/// </summary>
/// <param name="rootServiceProvider">The root <see cref="IServiceProvider"/> from the DI container.</param>
/// <param name="registry">The <see cref="MutableServiceRegistry"/> for per-test types.</param>
/// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
internal sealed class TestClientArtifactsActivator(
    IServiceProvider rootServiceProvider,
    MutableServiceRegistry registry,
    ILoggerFactory loggerFactory) : IClientArtifactsActivator
{
#pragma warning disable CA2000 // The FallbackServiceProvider lives for the singleton's lifetime
    readonly ClientArtifactsActivator _inner = new(
        new FallbackServiceProvider(rootServiceProvider, registry),
        loggerFactory);
#pragma warning restore CA2000

    /// <inheritdoc/>
    public Catch<ActivatedArtifact> Activate(Type artifactType) => _inner.Activate(artifactType);

    /// <inheritdoc/>
    public Catch<ActivatedArtifact> Activate(IServiceProvider scopedServiceProvider, Type artifactType) =>
        _inner.Activate(WrapProvider(scopedServiceProvider), artifactType);

    /// <inheritdoc/>
    public Catch<ActivatedArtifact<T>> Activate<T>(Type artifactType)
        where T : class => _inner.Activate<T>(artifactType);

    /// <inheritdoc/>
    public Catch<ActivatedArtifact<T>> Activate<T>(IServiceProvider scopedServiceProvider, Type artifactType)
        where T : class => _inner.Activate<T>(WrapProvider(scopedServiceProvider), artifactType);

    /// <inheritdoc/>
    public Catch<object> ActivateNonDisposable(Type artifactType) => _inner.ActivateNonDisposable(artifactType);

    /// <inheritdoc/>
    public Catch<T> ActivateNonDisposable<T>(Type artifactType)
        where T : class => _inner.ActivateNonDisposable<T>(artifactType);

    IServiceProvider WrapProvider(IServiceProvider provider) =>
        provider is FallbackServiceProvider ? provider : new FallbackServiceProvider(provider, registry);
}
