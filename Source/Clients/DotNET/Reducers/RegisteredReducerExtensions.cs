// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Provides unconstrained siblings of the legacy marker-based reducer operations for plain registered classes.
/// </summary>
public static class RegisteredReducerExtensions
{
    /// <summary>
    /// Registers a reducer with its inferred read model.
    /// </summary>
    /// <typeparam name="T">The reducer class.</typeparam>
    /// <param name="reducers">The owning store's reducer registry.</param>
    /// <returns>The registered handler.</returns>
    public static Task<IReducerHandler> Register<T>(this IReducers reducers)
        where T : class => reducers.Register(typeof(T));

    /// <summary>
    /// Gets a registered handler for a reducer CLR type.
    /// </summary>
    /// <typeparam name="T">The reducer class.</typeparam>
    /// <param name="reducers">The owning store's reducer registry.</param>
    /// <returns>The registered handler.</returns>
    public static IReducerHandler GetHandlerFor<T>(this IReducers reducers)
        where T : class => reducers.GetHandlerFor(typeof(T));

    /// <summary>
    /// Gets failed partitions for a registered reducer CLR type.
    /// </summary>
    /// <typeparam name="T">The reducer class.</typeparam>
    /// <param name="reducers">The owning store's reducer registry.</param>
    /// <returns>The failed partitions.</returns>
    public static Task<IEnumerable<FailedPartition>> GetFailedPartitionsFor<T>(this IReducers reducers)
        where T : class => reducers.GetFailedPartitionsFor(typeof(T));

    /// <summary>
    /// Gets a registered reducer's state.
    /// </summary>
    /// <typeparam name="T">The reducer class.</typeparam>
    /// <param name="reducers">The owning store's reducer registry.</param>
    /// <returns>The reducer state.</returns>
    public static Task<ReducerState> GetStateFor<T>(this IReducers reducers)
        where T : class => reducers.GetStateFor(typeof(T));

    /// <summary>
    /// Replays a registered reducer CLR type.
    /// </summary>
    /// <typeparam name="T">The reducer class.</typeparam>
    /// <param name="reducers">The owning store's reducer registry.</param>
    /// <returns>The replay job identifier.</returns>
    public static Task<JobId> Replay<T>(this IReducers reducers)
        where T : class => reducers.Replay(typeof(T));
}
