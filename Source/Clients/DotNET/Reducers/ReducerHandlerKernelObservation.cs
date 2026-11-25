// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Defines which reducers receive authoritative read-model changes from the kernel.
/// </summary>
internal static class ReducerHandlerKernelObservation
{
    /// <summary>
    /// Determines whether the handler is registered for kernel observation.
    /// </summary>
    /// <param name="handler">The reducer handler.</param>
    /// <returns>Whether the active handler has event types to observe.</returns>
    internal static bool IsObservedByKernel(this IReducerHandler handler) => handler.IsActive && handler.EventTypes.Any();
}
