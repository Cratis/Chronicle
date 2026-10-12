// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Selects the declaration adapter without changing the shared reducer engine.
/// </summary>
internal static class ReducerInvokerSelector
{
    /// <summary>
    /// Creates the declaration adapter for the owning registry.
    /// </summary>
    /// <param name="descriptor">The reducer descriptor.</param>
    /// <param name="eventTypes">The owning registry.</param>
    /// <param name="activator">The classic artifact activator.</param>
    /// <param name="containerName">The model container.</param>
    /// <returns>The shared-engine invoker.</returns>
    internal static IReducerInvoker Create(ReducerDescriptor descriptor, IEventTypes eventTypes, IClientArtifactsActivator activator, ReadModelContainerName containerName) =>
        descriptor.IsSelfReducing
            ? new SelfReducerInvoker(eventTypes, descriptor.ReadModelType, containerName)
            : new ReducerInvoker(eventTypes, activator, descriptor.ReducerType, descriptor.ReadModelType, containerName);
}
