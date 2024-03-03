// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Kernel.Contracts.Primitives;

/// <summary>
/// Exception that gets thrown when no value is set for a OneOf construct.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="NoValueSetForOneOf"/> class.
/// </remarks>
/// <param name="types">Types for the one of.</param>
public class NoValueSetForOneOf(params Type[] types) : Exception($"No value set for OneOf<{string.Join(", ", types.Select(_ => _.Name))}>")
{
}
