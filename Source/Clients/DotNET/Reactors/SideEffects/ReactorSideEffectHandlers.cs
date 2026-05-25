// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.SideEffects;

/// <summary>
/// Represents an implementation of <see cref="IReactorSideEffectHandlers"/> that dispatches
/// reactor return values to all registered <see cref="IReactorSideEffectHandler"/> instances.
/// </summary>
/// <param name="handlers">All registered <see cref="IReactorSideEffectHandler"/> instances.</param>
public class ReactorSideEffectHandlers(IEnumerable<IReactorSideEffectHandler> handlers) : IReactorSideEffectHandlers
{
    /// <inheritdoc/>
    public bool CanHandle(EventContext eventContext, object value) =>
        handlers.Any(h => h.CanHandle(eventContext, value));

    /// <inheritdoc/>
    public async Task Handle(EventContext eventContext, IEventStore eventStore, object value)
    {
        foreach (var handler in handlers.Where(h => h.CanHandle(eventContext, value)))
        {
            await handler.Handle(eventContext, eventStore, value);
        }
    }
}
