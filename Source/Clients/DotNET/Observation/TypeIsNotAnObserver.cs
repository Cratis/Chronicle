// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Observation;

/// <summary>
/// Exception that gets thrown when a type is not an observer.
/// </summary>
public class TypeIsNotAnObserver : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="TypeIsNotAnObserver"/>.
    /// </summary>
    /// <param name="type">The Type that is not an observer.</param>
    public TypeIsNotAnObserver(Type type)
        : base($"Type '{type}' is not an observer")
    {
    }
}
