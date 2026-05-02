// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Exception that gets thrown when a reactor observation stream fails unexpectedly.
/// </summary>
/// <param name="reactorId">The <see cref="ReactorId"/> for the failing reactor.</param>
/// <param name="innerException">The underlying exception.</param>
public class ReactorObservationStreamFailed(ReactorId reactorId, Exception innerException)
    : Exception($"Observation stream failed unexpectedly for reactor '{reactorId}'.", innerException);