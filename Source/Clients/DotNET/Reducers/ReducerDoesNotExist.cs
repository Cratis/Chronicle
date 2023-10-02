// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Reducers;

/// <summary>
/// Exception that gets thrown when an observer does not exist.
/// </summary>
public class ReducerDoesNotExist : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="ReducerDoesNotExist"/>.
    /// </summary>
    /// <param name="reducer">The invalid <see cref="ReducerId"/>.</param>
    public ReducerDoesNotExist(ReducerId reducer)
        : base($"Observer with id '{reducer}' does not exist")
    {
    }

    /// <summary>
    /// Throw if the observer does not exist.
    /// </summary>
    /// <param name="reducerId">The <see cref="ReducerId"/> of the observer.</param>
    /// <param name="reducer">The possible null <see cref="IReducerHandler"/> >value to check.</param>
    /// <exception cref="ReducerDoesNotExist">Thrown if the observer handler value is null.</exception>
    public static void ThrowIfDoesNotExist(ReducerId reducerId, IReducerHandler? reducer)
    {
        if (reducer is null) throw new ReducerDoesNotExist(reducerId);
    }
}
