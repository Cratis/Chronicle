// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Observation;

/// <summary>
/// Exception that gets thrown when an observer identifier is unknown.
/// </summary>
public class UnknownObserverId : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="UnknownObserverType"/>.
    /// </summary>
    /// <param name="observerId">The identifier of the unknown observer.</param>
    public UnknownObserverId(ObserverId observerId)
        : base($"Observer with identifier '{observerId}' is not a known observer")
    {
    }
}
