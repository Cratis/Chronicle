// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Determines whether a reducer has an observer running on the kernel.
/// Reducers.ShouldReducerBeActive folds passive read models into IsActive, and
/// Reducers.RegisterWhenDispatchable requires event types. The kernel's client Reducer
/// subscribes its observer only when definition.IsActive is true.
/// </summary>
internal static class ReducerHandlerKernelObservation
{
    /// <summary>
    /// Checks whether the handler is active and has dispatchable events.
    /// </summary>
    /// <param name="handler">The reducer handler to check.</param>
    /// <returns>Whether the kernel observes events for the handler.</returns>
    internal static bool IsObservedByKernel(this IReducerHandler handler) => handler.IsActive && handler.EventTypes.Any();
}
