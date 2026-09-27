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
    /// <returns>The <see cref="JobId"/> of the replay job that was started or resumed, or <see cref="JobId.NotSet"/> if the reactor is not replayable.</returns>
    Task<JobId> Replay<TReactor>()
        where TReactor : IReactor;

    /// <summary>
    /// Replay a specific reactor by its identifier.
    /// </summary>
    /// <param name="reactorId"><see cref="ReactorId"/> to replay.</param>
    /// <returns>The <see cref="JobId"/> of the replay job that was started or resumed, or <see cref="JobId.NotSet"/> if the reactor is not replayable.</returns>
    Task<JobId> Replay(ReactorId reactorId);
}
