// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Jobs;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Provides unconstrained siblings of the legacy marker-based reactor operations for plain registered classes.
/// </summary>
public static class RegisteredReactorExtensions
{
    /// <summary>
    /// Registers a reactor type using this store's declarations.
    /// </summary>
    /// <typeparam name="T">The reactor class.</typeparam>
    /// <param name="reactors">The owning store's reactor registry.</param>
    /// <returns>The registered handler.</returns>
    public static Task<IReactorHandler> Register<T>(this IReactors reactors)
        where T : class => reactors.Register(typeof(T));

    /// <summary>
    /// Gets the registered handler for a reactor CLR type.
    /// </summary>
    /// <typeparam name="T">The reactor class.</typeparam>
    /// <param name="reactors">The owning store's reactor registry.</param>
    /// <returns>The registered handler.</returns>
    public static IReactorHandler GetHandlerFor<T>(this IReactors reactors)
        where T : class => reactors.GetHandlerFor(typeof(T));

    /// <summary>
    /// Gets a registered reactor's state.
    /// </summary>
    /// <typeparam name="T">The reactor class.</typeparam>
    /// <param name="reactors">The owning store's reactor registry.</param>
    /// <returns>The reactor state.</returns>
    public static Task<ReactorState> GetStateFor<T>(this IReactors reactors)
        where T : class => reactors.GetStateFor(typeof(T));

    /// <summary>
    /// Replays a registered reactor CLR type.
    /// </summary>
    /// <typeparam name="T">The reactor class.</typeparam>
    /// <param name="reactors">The owning store's reactor registry.</param>
    /// <returns>The replay job identifier.</returns>
    public static Task<JobId> Replay<T>(this IReactors reactors)
        where T : class => reactors.Replay(typeof(T));
}
