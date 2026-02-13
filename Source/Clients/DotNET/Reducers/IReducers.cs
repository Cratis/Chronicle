// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Defines a system for working with reducer registrations for the Kernel.
/// </summary>
public interface IReducers
{
    /// <summary>
    /// Discover all reducers from the entry assembly and dependencies.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Discover();

    /// <summary>
    /// Register all reducers with Chronicle.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Register();

    /// <summary>
    /// Registers a <typeparamref name="TReducer"/> reducer with Chronicle.
    /// </summary>
    /// <typeparam name="TReducer">The reactor type.</typeparam>
    /// <typeparam name="TReadModel">The model type the reducer is for.</typeparam>
    /// <returns>Awaitable task.</returns>
    Task<IReducerHandler> Register<TReducer, TReadModel>()
        where TReducer : IReducerFor<TReadModel>
        where TReadModel : class;

    /// <summary>
    /// Check if there is a reducer for a specific model type.
    /// </summary>
    /// <param name="readModelType">Model type to check for.</param>
    /// <returns>True if it has, false if not.</returns>
    bool HasReducerFor(Type readModelType);

    /// <summary>
    /// Check if there is a reducer for a specific read model type.
    /// </summary>
    /// <typeparam name="TReadModel">The read model type to check for.</typeparam>
    /// <returns>True if it has, false if not.</returns>
    bool HasFor<TReadModel>();

    /// <summary>
    /// Check if there is a reducer for a specific read model type.
    /// </summary>
    /// <param name="readModelType">The read model type to check for.</param>
    /// <returns>True if it has, false if not.</returns>
    bool HasFor(Type readModelType);

    /// <summary>
    /// Get all registered handlers.
    /// </summary>
    /// <returns>Collection of <see cref="IReducerHandler"/>.</returns>
    IEnumerable<IReducerHandler> GetAllHandlers();

    /// <summary>
    /// Get all registered reducers by its identifier.
    /// </summary>
    /// <param name="reducerId">The identifier of the reducer to get.</param>
    /// <returns><see cref="IReducerHandler"/> instance.</returns>
    IReducerHandler GetHandlerById(ReducerId reducerId);

    /// <summary>
    /// Get a specific handler for a specific model type.
    /// </summary>
    /// <param name="readModelType">Model type to get for.</param>
    /// <returns><see cref="IReducerHandler"/> instance.</returns>
    IReducerHandler GetHandlerForReadModelType(Type readModelType);

    /// <summary>
    /// Gets a specific handler by its reducer type.
    /// </summary>
    /// <param name="reducerType">The reducer type to get for.</param>
    /// <returns><see cref="IReducerHandler"/>.</returns>
    IReducerHandler GetHandlerFor(Type reducerType);

    /// <summary>
    /// Get a specific handler for a specific reducer.
    /// </summary>
    /// <typeparam name="TReducer">Type of reducer to get for.</typeparam>
    /// <returns><see cref="IReducerHandler"/>.</returns>
    IReducerHandler GetHandlerFor<TReducer>()
        where TReducer : IReducer;

    /// <summary>
    /// Get the CLR type for a specific reducer.
    /// </summary>
    /// <param name="reducerId"><see cref="ReducerId"/> to get for.</param>
    /// <returns>The type.</returns>
    Type GetClrType(ReducerId reducerId);

    /// <summary>
    /// Get any failed partitions for a specific reducer.
    /// </summary>
    /// <typeparam name="TReducer">Type of reducer.</typeparam>
    /// <returns>Collection of <see cref="FailedPartition"/>, if any.</returns>
    Task<IEnumerable<FailedPartition>> GetFailedPartitionsFor<TReducer>()
        where TReducer : IReducer;

    /// <summary>
    /// Get any failed partitions for a specific reducer.
    /// </summary>
    /// <param name="reducerType">Type of reducer.</param>
    /// <returns>Collection of <see cref="FailedPartition"/>, if any.</returns>
    Task<IEnumerable<FailedPartition>> GetFailedPartitionsFor(Type reducerType);

    /// <summary>
    /// Get the state of a specific reactor.
    /// </summary>
    /// <typeparam name="TReducer">Type of reactor get for.</typeparam>
    /// <returns><see cref="ReducerState"/>.</returns>
    Task<ReducerState> GetStateFor<TReducer>()
        where TReducer : IReducer;

    /// <summary>
    /// Replay a specific reducer.
    /// </summary>
    /// <typeparam name="TReducer">Type of reducer to replay.</typeparam>
    /// <returns>Awaitable task.</returns>
    Task Replay<TReducer>()
        where TReducer : IReducer;

    /// <summary>
    /// Replay a specific reducer by its identifier.
    /// </summary>
    /// <param name="reducerId"><see cref="ReducerId"/> to replay.</param>
    /// <returns>Awaitable task.</returns>
    Task Replay(ReducerId reducerId);
}
