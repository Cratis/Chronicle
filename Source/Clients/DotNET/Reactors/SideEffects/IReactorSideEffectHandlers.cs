// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Chronicle.Reactors.SideEffects;

/// <summary>
/// Defines a system for managing <see cref="IReactorSideEffectHandler"/> instances and dispatching return values to them.
/// </summary>
public interface IReactorSideEffectHandlers
{
    /// <summary>
    /// Determines whether any registered handler can process the given return value.
    /// </summary>
    /// <param name="reactorContext">The <see cref="ReactorContext"/> for the reactor invocation.</param>
    /// <param name="value">The value returned by the reactor handler method.</param>
    /// <returns><see langword="true"/> if any handler can process the value; otherwise <see langword="false"/>.</returns>
    bool CanHandle(ReactorContext reactorContext, object value);

    /// <summary>
    /// Dispatches the return value to all handlers that can process it.
    /// </summary>
    /// <param name="reactorContext">The <see cref="ReactorContext"/> for the reactor invocation.</param>
    /// <param name="eventStore">The <see cref="IEventStore"/> to use for appending events.</param>
    /// <param name="value">The value returned by the reactor handler method.</param>
    /// <returns>A <see cref="Task{TResult}"/> with a <see cref="Result{TError}"/> indicating success or containing <see cref="ReactorSideEffectFailure"/> details.</returns>
    Task<Result<ReactorSideEffectFailure>> Handle(ReactorContext reactorContext, IEventStore eventStore, object value);
}
