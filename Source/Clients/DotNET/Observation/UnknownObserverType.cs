// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Exception that gets thrown when a type is not an observer.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="UnknownObserverType"/>.
/// </remarks>
/// <param name="type">The Type that is not an observer.</param>
public class UnknownObserverType(Type type) : Exception($"Type '{type}' is not a known observer")
{
}
