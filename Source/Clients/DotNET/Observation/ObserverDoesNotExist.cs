// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Exception that gets thrown when an observer does not exist.
/// </summary>
public class ObserverDoesNotExist : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="ObserverDoesNotExist"/>.
    /// </summary>
    /// <param name="observerId">The invalid <see cref="ObserverId"/>.</param>
    public ObserverDoesNotExist(ObserverId observerId)
        : base($"Observer with id '{observerId}' does not exist")
    {
    }

    /// <summary>
    /// Throw if the observer does not exist.
    /// </summary>
    /// <param name="observerId">The <see cref="ObserverId"/> of the observer.</param>
    /// <param name="observer">The possible null <see cref="ObserverHandler"/> >value to check.</param>
    /// <exception cref="ObserverDoesNotExist">Thrown if the observer handler value is null.</exception>
    public static void ThrowIfDoesNotExist(ObserverId observerId, ObserverHandler? observer)
    {
        if (observer is null) throw new ObserverDoesNotExist(observerId);
    }
}
