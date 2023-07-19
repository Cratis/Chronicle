// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Reducers;

/// <summary>
/// Defines a reducer.
/// </summary>
/// <typeparam name="TModel"></typeparam>
public interface IReducer<TModel>
{
    /// <summary>
    /// Gets the <see cref="ReducerId"/> for the reducer.
    /// </summary>
    ReducerId Id { get; }
}
