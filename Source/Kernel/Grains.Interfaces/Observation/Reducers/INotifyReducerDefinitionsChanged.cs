// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.Observation.Reducers;

/// <summary>
/// Defines an internal grain observer that lets you get notified when definitions change on a reducer.
/// </summary>
public interface INotifyReducerDefinitionsChanged : IGrainObserver
{
    /// <summary>
    /// Called when reducer definition is changed.
    /// </summary>
    void OnReducerDefinitionsChanged();
}
