// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Exception that gets thrown when an observer identifier is unknown.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="UnknownObserverType"/>.
/// </remarks>
/// <param name="observerId">The identifier of the unknown observer.</param>
public class UnknownObserverId(ObserverId observerId) : Exception($"Observer with identifier '{observerId}' is not a known observer")
{
}
