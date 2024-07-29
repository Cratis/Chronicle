// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Exception that gets thrown when an reducer does not exist.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="ReducerDoesNotExist"/>.
/// </remarks>
/// <param name="reducer">The invalid <see cref="ReducerId"/>.</param>
public class ReducerDoesNotExist(ReducerId reducer) : Exception($"Reducer with id '{reducer}' does not exist")
{
    /// <summary>
    /// Throw if the reducer does not exist.
    /// </summary>
    /// <param name="reducerId">The <see cref="ReducerId"/> of the reducer.</param>
    /// <param name="reducer">The possible null <see cref="IReducerHandler"/> >value to check.</param>
    /// <exception cref="ReducerDoesNotExist">Thrown if the reducer handler value is null.</exception>
    public static void ThrowIfDoesNotExist(ReducerId reducerId, IReducerHandler? reducer)
    {
        if (reducer is null) throw new ReducerDoesNotExist(reducerId);
    }
}
