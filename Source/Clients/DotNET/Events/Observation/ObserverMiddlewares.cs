// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Types;

namespace Aksio.Cratis.Events.Observation;

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
    /// <param name="middlewares"><see cref="IInstancesOf{T}"/> of <see cref="IObserverMiddleware"/>.</param>
    public ObserverMiddlewares(IInstancesOf<IObserverMiddleware> middlewares)
    {
        _all = middlewares.ToArray();
    }

    /// <inheritdoc/>
    public Task BeforeInvoke(EventContext eventContext, object @event) => Task.WhenAll(_all.Select(_ => _.BeforeInvoke(eventContext, @event)));

    /// <inheritdoc/>
    public Task AfterInvoke(EventContext eventContext, object @event) => Task.WhenAll(_all.Select(_ => _.AfterInvoke(eventContext, @event)));
}
