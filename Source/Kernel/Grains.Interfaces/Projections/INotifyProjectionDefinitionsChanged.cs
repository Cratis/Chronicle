// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.Projections;

/// <summary>
/// Defines an internal grain observer that lets you get notified when definitions change on a projection.
/// </summary>
public interface INotifyProjectionDefinitionsChanged : IGrainObserver
{
    /// <summary>
    /// Called when a projection definition is changed.
    /// </summary>
    void OnProjectionDefinitionsChanged();
}
