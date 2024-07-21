// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Reactions;

/// <summary>
/// Represents an implementation of <see cref="IReactionMiddlewares"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ReactionMiddlewares"/> class.
/// </remarks>
/// <param name="clientArtifacts">Optional <see cref="IClientArtifactsProvider"/> for the client artifacts.</param>
/// <param name="serviceProvider"><see cref="IServiceProvider"/> for resolving instances.</param>
[Singleton]
public class ReactionMiddlewares(
    IClientArtifactsProvider clientArtifacts,
    IServiceProvider serviceProvider) : IReactionMiddlewares
{
    readonly IEnumerable<IReactionMiddleware> _all = clientArtifacts.ReactionMiddlewares.Select(_ => (serviceProvider.GetRequiredService(_) as IReactionMiddleware)!).ToArray();

    /// <inheritdoc/>
    public Task BeforeInvoke(EventContext eventContext, object @event) => Task.WhenAll(_all.Select(_ => _.BeforeInvoke(eventContext, @event)));

    /// <inheritdoc/>
    public Task AfterInvoke(EventContext eventContext, object @event) => Task.WhenAll(_all.Select(_ => _.AfterInvoke(eventContext, @event)));
}
