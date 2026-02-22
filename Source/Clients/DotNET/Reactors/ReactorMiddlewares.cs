// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Represents an implementation of <see cref="IReactorMiddlewares"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ReactorMiddlewares"/> class.
/// </remarks>
/// <param name="activatedMiddlewares">The <see cref="ActivatedArtifact{Middleware}"/> of <see cref="IReactorMiddleware"/>.</param>
[IgnoreConvention]
public class ReactorMiddlewares(ActivatedArtifact<IReactorMiddleware>[] activatedMiddlewares) : IReactorMiddlewares
{
    /// <inheritdoc/>
    public Task BeforeInvoke(EventContext eventContext, object @event)
    {
        if (activatedMiddlewares.Length == 0)
        {
            return Task.CompletedTask;
        }
        return Task.WhenAll(activatedMiddlewares.Select(_ => _.Instance.BeforeInvoke(eventContext, @event)));
    }

    /// <inheritdoc/>
    public Task AfterInvoke(EventContext eventContext, object @event)
    {
        if (activatedMiddlewares.Length == 0)
        {
            return Task.CompletedTask;
        }
        return Task.WhenAll(activatedMiddlewares.Select(_ => _.Instance.AfterInvoke(eventContext, @event)));
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (activatedMiddlewares.Length == 0)
        {
            return;
        }
        foreach (var middleware in activatedMiddlewares)
        {
            await middleware.DisposeAsync();
        }
    }
}
