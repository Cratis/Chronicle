// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Contracts.Primitives;

/// <summary>
/// Exception that gets thrown when no value is set for a OneOf construct.
/// </summary>
public class NoValueSetForOneOf : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoValueSetForOneOf"/> class.
    /// </summary>
    /// <param name="types">Types for the one of.</param>
    public NoValueSetForOneOf(params Type[] types) : base($"No value set for OneOf<{string.Join(", ", types.Select(_ => _.Name))}>")
    {
    }
}
