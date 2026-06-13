// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Types;

namespace Cratis.Chronicle.Reactors.SideEffects;

/// <summary>
/// Represents an implementation of <see cref="IReactorSideEffectHandlers"/> that dispatches
/// reactor return values to all registered <see cref="IReactorSideEffectHandler"/> instances.
/// </summary>
/// <param name="handlers">All registered <see cref="IReactorSideEffectHandler"/> instances.</param>
[Singleton]
public class ReactorSideEffectHandlers(IInstancesOf<IReactorSideEffectHandler> handlers) : IReactorSideEffectHandlers
{
    /// <inheritdoc/>
    public bool CanHandle(ReactorContext reactorContext, object value) =>
        handlers.Any(h => h.CanHandle(reactorContext, value));

    /// <inheritdoc/>
    public async Task<Result<ReactorSideEffectFailure>> Handle(ReactorContext reactorContext, IEventStore eventStore, object value)
    {
        foreach (var handler in handlers.Where(h => h.CanHandle(reactorContext, value)))
        {
            var result = await handler.Handle(reactorContext, eventStore, value);
            if (result.TryGetError(out var failure))
            {
                return Result.Failed(failure);
            }
        }

        return Result.Success<ReactorSideEffectFailure>();
    }
}
