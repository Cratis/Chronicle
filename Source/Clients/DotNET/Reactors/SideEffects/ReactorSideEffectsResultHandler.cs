// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.SideEffects;

/// <summary>
/// Handles a collection of <see cref="ReactorSideEffect"/> instances returned from a reactor handler method.
/// </summary>
public class ReactorSideEffectsResultHandler : IReactorSideEffectHandler
{
    /// <inheritdoc/>
    public bool CanHandle(ReactorContext reactorContext, object value) => value is IEnumerable<ReactorSideEffect>;

    /// <inheritdoc/>
    public async Task Handle(ReactorContext reactorContext, IEventStore eventStore, object value)
    {
        foreach (var sideEffect in (IEnumerable<ReactorSideEffect>)value)
        {
            await ReactorSideEffectResultHandler.AppendSideEffect(sideEffect, reactorContext, eventStore);
        }
    }
}
