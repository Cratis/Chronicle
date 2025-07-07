// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    Task<ReactorHandler> Register<TReactor>()
        where TReactor : IReactor;

    /// <summary>
    /// Gets a specific handler by its <typeparamref name="TReactor"/> type.
    /// </summary>
    /// <typeparam name="TReactor">The reactor type.</typeparam>
    /// <returns><see cref="ReactorHandler"/> instance.</returns>
    ReactorHandler GetHandlerFor<TReactor>()
        where TReactor : IReactor;

    /// <summary>
    /// Gets a specific handler by its <see cref="ReactorId"/>.
    /// </summary>
    /// <param name="id"><see cref="ReactorId"/> to get for.</param>
    /// <returns><see cref="ReactorHandler"/> instance.</returns>
    ReactorHandler GetHandlerById(ReactorId id);

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
    /// <returns>Awaitable task.</returns>
    Task Replay<TReactor>()
        where TReactor : IReactor;

    /// <summary>
    /// Replay a specific reactor by its identifier.
    /// </summary>
    /// <param name="reactorId"><see cref="ReactorId"/> to replay.</param>
    /// <returns>Awaitable task.</returns>
    Task Replay(ReactorId reactorId);
}
