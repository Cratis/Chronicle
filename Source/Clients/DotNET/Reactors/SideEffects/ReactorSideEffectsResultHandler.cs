// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.SideEffects;

/// <summary>
/// Handles a collection of <see cref="ReactorSideEffect"/> instances returned from a reactor handler method.
/// </summary>
public class ReactorSideEffectsResultHandler : IReactorSideEffectHandler
{
    /// <inheritdoc/>
    public bool CanHandle(EventContext eventContext, object value) => value is IEnumerable<ReactorSideEffect>;

    /// <inheritdoc/>
    public async Task Handle(EventContext eventContext, IEventStore eventStore, object value)
    {
        foreach (var sideEffect in (IEnumerable<ReactorSideEffect>)value)
        {
            await ReactorSideEffectResultHandler.AppendSideEffect(sideEffect, eventContext, eventStore);
        }
    }
}
