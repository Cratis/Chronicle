// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Aksio.Cratis.Observation;

/// <summary>
/// Represents an implementation of <see cref="IObserverMiddlewares"/>.
/// </summary>
[Singleton]
public class ObserverMiddlewares : IObserverMiddlewares
{
    readonly IEnumerable<IObserverMiddleware> _all;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObserverMiddlewares"/> class.
    /// </summary>
    /// <param name="clientArtifacts">Optional <see cref="IClientArtifactsProvider"/> for the client artifacts.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for resolving instances.</param>
    public ObserverMiddlewares(
        IClientArtifactsProvider clientArtifacts,
        IServiceProvider serviceProvider)
    {
        _all = clientArtifacts.ObserverMiddlewares.Select(_ => (serviceProvider.GetRequiredService(_) as IObserverMiddleware)!).ToArray();
    }

    /// <inheritdoc/>
    public Task BeforeInvoke(EventContext eventContext, object @event) => Task.WhenAll(_all.Select(_ => _.BeforeInvoke(eventContext, @event)));

    /// <inheritdoc/>
    public Task AfterInvoke(EventContext eventContext, object @event) => Task.WhenAll(_all.Select(_ => _.AfterInvoke(eventContext, @event)));
}
