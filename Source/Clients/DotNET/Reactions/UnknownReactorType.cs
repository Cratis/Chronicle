// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Exception that gets thrown when a type is not an Reactor.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="UnknownReactorType"/>.
/// </remarks>
/// <param name="type">The Type that is not an Reactor.</param>
public class UnknownReactorType(Type type) : Exception($"Type '{type}' is not a known Reactor")
{
}
