// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Observation;

/// <summary>
/// Exception that gets thrown when a type is not a reducer.
/// </summary>
public class UnknownReducerType : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="UnknownReducerType"/>.
    /// </summary>
    /// <param name="type">The Type that is not an reducer.</param>
    public UnknownReducerType(Type type)
        : base($"Type '{type}' is not a known reducer")
    {
    }
}
