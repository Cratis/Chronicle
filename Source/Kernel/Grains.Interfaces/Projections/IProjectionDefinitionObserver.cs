// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans;

namespace Aksio.Cratis.Kernel.Grains.Projections;

/// <summary>
/// Defines an internal grain observer that lets you get notified when definitions change on a projection.
/// </summary>
public interface IProjectionDefinitionObserver : IGrainObserver
{
    /// <summary>
    /// Called when either the projection or the pipeline definition is changed.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    void OnDefinitionsChanged();
}
