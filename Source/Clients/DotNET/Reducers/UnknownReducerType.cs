// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Exception that gets thrown when a type is not a reducer.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="UnknownReducerType"/>.
/// </remarks>
/// <param name="type">The Type that is not an reducer.</param>
public class UnknownReducerType(Type type) : Exception($"Type '{type}' is not a known reducer")
{
}
