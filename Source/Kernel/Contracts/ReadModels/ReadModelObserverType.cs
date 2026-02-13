// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.ReadModels;

/// <summary>
/// Represents the type of owner for a read model.
/// </summary>
public enum ReadModelObserverType
{
    /// <summary>
    /// The read model observer type is not set.
    /// </summary>
    NotSet = 0,

    /// <summary>
    /// The read model is owned by a reducer.
    /// </summary>
    Reducer = 1,

    /// <summary>
    /// The read model is owned by a projection.
    /// </summary>
    Projection = 2
}
