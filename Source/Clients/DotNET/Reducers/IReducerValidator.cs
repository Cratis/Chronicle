// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Reducers;

/// <summary>
/// Defines a validator for reducers.
/// </summary>
public interface IReducerValidator
{
    /// <summary>
    /// Validate a reducer type.
    /// </summary>
    /// <param name="reducerType">Type of <see cref="IReducerFor{T}"/>.</param>
    void Validate(Type reducerType);
}
