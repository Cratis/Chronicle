// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Auditing;

/// <summary>
/// Exception that gets thrown when a <see cref="CausedById"/> is not found.
/// </summary>
public class UnknownCausedByIdentifier : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnknownCausedByIdentifier"/> class.
    /// </summary>
    /// <param name="causedById">The missing <see cref="CausedById"/>.</param>
    public UnknownCausedByIdentifier(CausedById causedById) : base($"Unknown caused by identifier '{causedById}'")
    {
    }
}
