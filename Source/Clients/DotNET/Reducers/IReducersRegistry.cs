// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Reducers;

/// <summary>
/// Defines a system that can register reducers.
/// </summary>
public interface IReducersRegistry
{
    /// <summary>
    /// Get all registered reducers by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the reducer to get.</param>
    /// <returns><see cref="IReducerHandler"/> instance.</returns>
    IReducerHandler GetById(ReducerId id);
}
