// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Represents an implementation of <see cref="IReactorMiddlewares"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ReactorMiddlewares"/> class.
/// </remarks>
/// <param name="clientArtifacts">Optional <see cref="IClientArtifactsProvider"/> for the client artifacts.</param>
/// <param name="serviceProvider"><see cref="IServiceProvider"/> for resolving instances.</param>
[Singleton]
public class ReactorMiddlewares(
    IClientArtifactsProvider clientArtifacts,
    IServiceProvider serviceProvider) : IReactorMiddlewares
{
    readonly IEnumerable<IReactorMiddleware> _all = clientArtifacts.ReactorMiddlewares
        .Select(_ => (ActivatorUtilities.CreateInstance(serviceProvider, _) as IReactorMiddleware)!).ToArray();

    /// <inheritdoc/>
    public Task BeforeInvoke(EventContext eventContext, object @event) => Task.WhenAll(_all.Select(_ => _.BeforeInvoke(eventContext, @event)));

    /// <inheritdoc/>
    public Task AfterInvoke(EventContext eventContext, object @event) => Task.WhenAll(_all.Select(_ => _.AfterInvoke(eventContext, @event)));
}
