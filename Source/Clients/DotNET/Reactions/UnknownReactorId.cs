// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Exception that gets thrown when an Reactor identifier is unknown.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="UnknownReactorId"/>.
/// </remarks>
/// <param name="reactorId">The identifier of the unknown reducer.</param>
public class UnknownReactorId(ReactorId reactorId) : Exception($"Reactor with identifier '{reactorId}' is not a known Reactor")
{
}
