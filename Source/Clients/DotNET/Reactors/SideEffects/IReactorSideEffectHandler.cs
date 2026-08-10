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
/// <see cref="CanHandle(ReactorContext, IEventStore, object)"/> method returns <see langword="true"/>.
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
    /// Determines whether this handler can process the given return value.
    /// </summary>
    /// <param name="reactorContext">The <see cref="ReactorContext"/> for the reactor invocation.</param>
    /// <param name="eventStore">The <see cref="IEventStore"/> the reactor is running under.</param>
    /// <param name="value">The value returned by the reactor handler method.</param>
    /// <returns><see langword="true"/> if this handler can process the value; otherwise <see langword="false"/>.</returns>
    /// <remarks>
    /// The event store is passed per call rather than captured, because everything it exposes — the event type
    /// registry in particular — belongs to the namespace the resolving scope named. A handler that holds one is
    /// answering for whichever namespace happened to build it first.
    /// <para>
    /// Defaults to the event-store-less overload so a handler written against the previous contract keeps
    /// working unchanged. Implementations that need event-store-specific state can override this overload while
    /// retaining the previous one for callers compiled against the published contract.
    /// </para>
    /// </remarks>
    bool CanHandle(ReactorContext reactorContext, IEventStore eventStore, object value) => CanHandle(reactorContext, value);

    /// <summary>
    /// Processes the return value, typically by appending one or more events to an event sequence.
    /// </summary>
    /// <param name="reactorContext">The <see cref="ReactorContext"/> for the reactor invocation.</param>
    /// <param name="eventStore">The <see cref="IEventStore"/> to use for appending events.</param>
    /// <param name="value">The value returned by the reactor handler method.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<Result<ReactorSideEffectFailure>> Handle(ReactorContext reactorContext, IEventStore eventStore, object value);
}
