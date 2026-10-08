// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Defines a system for working with Reactor registrations for the Kernel.
/// </summary>
public interface IReactors
{
    /// <summary>
    /// Discover all Reactors from the entry assembly and dependencies.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Discover();

    /// <summary>
    /// Register all Reactors with Chronicle.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Register();

    /// <summary>
    /// Registers a <typeparamref name="TReactor"/> reactor with Chronicle.
    /// </summary>
    /// <typeparam name="TReactor">The reactor type.</typeparam>
    /// <returns>Awaitable task.</returns>
    Task<IReactorHandler> Register<TReactor>()
        where TReactor : IReactor;

    /// <summary>
    /// Registers a reactor from an explicit event subscription and an awaited callback.
    /// </summary>
    /// <param name="id">The stable reactor identifier.</param>
    /// <param name="configure">Configures the event types, sequence, and replay policy.</param>
    /// <param name="handle">Handles each delivered event; successful completion acknowledges the event.</param>
    /// <returns>The registered handler, which can be inspected for state and failed partitions.</returns>
    /// <remarks>Delegate handlers report <see cref="IReactorHandler.ReactorType"/> as <see cref="object"/>.</remarks>
    /// <exception cref="NoEventTypesForReactor">Thrown when no event types are configured.</exception>
    /// <exception cref="ReactorAlreadyRegistered">Thrown when the identifier already belongs to a reactor in this client.</exception>
    Task<IReactorHandler> Register(ReactorId id, Action<IReactorDefinitionBuilder> configure, Func<ReactorEvent, CancellationToken, Task> handle);

    /// <summary>
    /// Define a reactor fluently, without registering it.
    /// </summary>
    /// <param name="id">The stable reactor identifier.</param>
    /// <param name="define">Declares the reactor's handlers, event sequence and replay policy on an <see cref="IReactorBuilder"/>.</param>
    /// <returns>The <see cref="IReactorDefinition"/>, which can be inspected and then registered with <see cref="Register(IReactorDefinition)"/>.</returns>
    /// <remarks>
    /// Event types are resolved against the event types this client knows at the time of the call, which is also what
    /// an all-events subscription is expanded to.
    /// </remarks>
    /// <exception cref="NoEventTypesForReactor">Thrown when the definition subscribes to no event types.</exception>
    /// <exception cref="Events.TypeIsNotAnEventType">Thrown when a typed handler names a type that is not a known event type.</exception>
    IReactorDefinition Define(ReactorId id, Action<IReactorBuilder> define);

    /// <summary>
    /// Define and register a reactor fluently, with typed and catch-all handlers.
    /// </summary>
    /// <param name="id">The stable reactor identifier.</param>
    /// <param name="define">Declares the reactor's handlers, event sequence and replay policy on an <see cref="IReactorBuilder"/>.</param>
    /// <returns>The registered handler, which can be inspected for state and failed partitions.</returns>
    /// <remarks>
    /// This is <see cref="Define"/> followed by <see cref="Register(IReactorDefinition)"/>.
    /// </remarks>
    /// <exception cref="NoEventTypesForReactor">Thrown when the definition subscribes to no event types.</exception>
    /// <exception cref="Events.TypeIsNotAnEventType">Thrown when a typed handler names a type that is not a known event type.</exception>
    /// <exception cref="ReactorAlreadyRegistered">Thrown when the identifier already belongs to a reactor in this client.</exception>
    Task<IReactorHandler> Register(ReactorId id, Action<IReactorBuilder> define);

    /// <summary>
    /// Register a reactor from a fluent definition.
    /// </summary>
    /// <param name="definition">The <see cref="IReactorDefinition"/> to register.</param>
    /// <returns>The registered handler, which can be inspected for state and failed partitions.</returns>
    /// <remarks>
    /// The reactor is an ordinary Chronicle reactor: it has an observer of its own, subscribed to exactly the
    /// definition's <see cref="IReactorDefinition.EventTypes"/>, and it has state, failed partitions and replay like a
    /// discovered reactor has. Each delivered event is deserialized to its CLR type and passed to
    /// <see cref="IReactorDefinition.Handle"/>. The registration is sent to Chronicle immediately, and is sent again
    /// whenever the connection is re-established. Delegate handlers report <see cref="IReactorHandler.ReactorType"/> as
    /// <see cref="object"/>.
    /// </remarks>
    /// <exception cref="ReactorAlreadyRegistered">Thrown when the identifier already belongs to a reactor in this client.</exception>
    Task<IReactorHandler> Register(IReactorDefinition definition);

    /// <summary>
    /// Unregisters a reactor in this client, disconnecting its observation stream. An unknown identifier is ignored.
    /// </summary>
    /// <param name="id">The reactor identifier to unregister.</param>
    void Unregister(ReactorId id);

    /// <summary>
    /// Gets a specific handler by its <typeparamref name="TReactor"/> type.
    /// </summary>
    /// <typeparam name="TReactor">The reactor type.</typeparam>
    /// <returns><see cref="ReactorHandler"/> instance.</returns>
    IReactorHandler GetHandlerFor<TReactor>()
        where TReactor : IReactor;

    /// <summary>
    /// Gets a specific handler by its <see cref="ReactorId"/>.
    /// </summary>
    /// <param name="id"><see cref="ReactorId"/> to get for.</param>
    /// <returns><see cref="ReactorHandler"/> instance.</returns>
    IReactorHandler GetHandlerById(ReactorId id);

    /// <summary>
    /// Get any failed partitions for a specific reactor.
    /// </summary>
    /// <typeparam name="TReactor">Type of reducer.</typeparam>
    /// <returns>Collection of <see cref="FailedPartition"/>, if any.</returns>
    Task<IEnumerable<FailedPartition>> GetFailedPartitionsFor<TReactor>();

    /// <summary>
    /// Get any failed partitions for a specific reactor.
    /// </summary>
    /// <param name="reactorType">Type of reducer.</param>
    /// <returns>Collection of <see cref="FailedPartition"/>, if any.</returns>
    Task<IEnumerable<FailedPartition>> GetFailedPartitionsFor(Type reactorType);

    /// <summary>
    /// Get the state of a specific reactor.
    /// </summary>
    /// <typeparam name="TReactor">Type of reactor get for.</typeparam>
    /// <returns><see cref="ReactorState"/>.</returns>
    Task<ReactorState> GetStateFor<TReactor>()
        where TReactor : IReactor;

    /// <summary>
    /// Replay a specific reactor.
    /// </summary>
    /// <typeparam name="TReactor">Type of reactor to replay.</typeparam>
    /// <returns>The <see cref="JobId"/> of the replay job that was started or resumed, or <see cref="JobId.NotSet"/> if the reactor is not replayable or its observer is not in a state it can replay from (disconnected, quarantined); reconnect or release it before replaying.</returns>
    Task<JobId> Replay<TReactor>()
        where TReactor : IReactor;

    /// <summary>
    /// Replay a specific reactor by its identifier.
    /// </summary>
    /// <param name="reactorId"><see cref="ReactorId"/> to replay.</param>
    /// <returns>The <see cref="JobId"/> of the replay job that was started or resumed, or <see cref="JobId.NotSet"/> if the reactor is not replayable or its observer is not in a state it can replay from (disconnected, quarantined); reconnect or release it before replaying.</returns>
    Task<JobId> Replay(ReactorId reactorId);
}
