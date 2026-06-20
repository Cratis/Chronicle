// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Chronicle.Reactors.SideEffects;

/// <summary>
/// Defines a handler that can process a value returned from a reactor handler method and produce side effects.
/// </summary>
/// <remarks>
/// Implement this interface and register it in the DI container to extend the set of return types
/// that reactor handler methods can produce. The framework discovers all registered
/// <see cref="IReactorSideEffectHandler"/> instances and delegates to those whose
/// <see cref="CanHandle"/> method returns <see langword="true"/>.
/// </remarks>
public interface IReactorSideEffectHandler
{
    /// <summary>
    /// Determines whether this handler can process the given return value.
    /// </summary>
    /// <param name="reactorContext">The <see cref="ReactorContext"/> for the reactor invocation.</param>
    /// <param name="value">The value returned by the reactor handler method.</param>
    /// <returns><see langword="true"/> if this handler can process the value; otherwise <see langword="false"/>.</returns>
    bool CanHandle(ReactorContext reactorContext, object value);

    /// <summary>
    /// Processes the return value, typically by appending one or more events to an event sequence.
    /// </summary>
    /// <param name="reactorContext">The <see cref="ReactorContext"/> for the reactor invocation.</param>
    /// <param name="eventStore">The <see cref="IEventStore"/> to use for appending events.</param>
    /// <param name="value">The value returned by the reactor handler method.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<Result<ReactorSideEffectFailure>> Handle(ReactorContext reactorContext, IEventStore eventStore, object value);
}
