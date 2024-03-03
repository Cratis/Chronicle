// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Observation;

/// <summary>
/// Exception that gets thrown when a type is not an observer.
/// </summary>
public class UnknownObserverType : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="UnknownObserverType"/>.
    /// </summary>
    /// <param name="type">The Type that is not an observer.</param>
    public UnknownObserverType(Type type)
        : base($"Type '{type}' is not a known observer")
    {
    }
}
