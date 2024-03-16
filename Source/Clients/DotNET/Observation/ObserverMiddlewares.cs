// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Observation;

/// <summary>
/// Represents an implementation of <see cref="IObserverMiddlewares"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ObserverMiddlewares"/> class.
/// </remarks>
/// <param name="clientArtifacts">Optional <see cref="IClientArtifactsProvider"/> for the client artifacts.</param>
/// <param name="serviceProvider"><see cref="IServiceProvider"/> for resolving instances.</param>
[Singleton]
public class ObserverMiddlewares(
    IClientArtifactsProvider clientArtifacts,
    IServiceProvider serviceProvider) : IObserverMiddlewares
{
    readonly IEnumerable<IObserverMiddleware> _all = clientArtifacts.ObserverMiddlewares.Select(_ => (serviceProvider.GetRequiredService(_) as IObserverMiddleware)!).ToArray();

    /// <inheritdoc/>
    public Task BeforeInvoke(EventContext eventContext, object @event) => Task.WhenAll(_all.Select(_ => _.BeforeInvoke(eventContext, @event)));

    /// <inheritdoc/>
    public Task AfterInvoke(EventContext eventContext, object @event) => Task.WhenAll(_all.Select(_ => _.AfterInvoke(eventContext, @event)));
}
