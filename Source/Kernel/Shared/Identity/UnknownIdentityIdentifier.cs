// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Identities;

/// <summary>
/// Exception that gets thrown when a <see cref="IdentityId"/> is not found.
/// </summary>
public class UnknownIdentityIdentifier : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnknownIdentityIdentifier"/> class.
    /// </summary>
    /// <param name="causedById">The missing <see cref="IdentityId"/>.</param>
    public UnknownIdentityIdentifier(IdentityId causedById) : base($"Unknown caused by identifier '{causedById}'")
    {
    }
}
