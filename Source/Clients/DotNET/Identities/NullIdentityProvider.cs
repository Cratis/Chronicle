// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Identities;

/// <summary>
/// Represents a null implementation of <see cref="IIdentityProvider"/>.
/// </summary>
public class NullIdentityProvider : IIdentityProvider
{
    /// <inheritdoc/>
    public Identity GetCurrent() => Identity.NotSet;
}
