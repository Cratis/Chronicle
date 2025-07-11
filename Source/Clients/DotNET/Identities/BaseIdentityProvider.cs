// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Identities;

/// <summary>
/// Represents a null implementation of <see cref="IIdentityProvider"/>.
/// </summary>
public class BaseIdentityProvider : IIdentityProvider
{
    static readonly AsyncLocal<Identity> _current = new();

    /// <inheritdoc/>
    Identity IIdentityProvider.GetCurrent() => GetCurrent();

    /// <inheritdoc/>
    public void SetCurrentIdentity(Identity identity) => _current.Value = identity;

    /// <inheritdoc/>
    public void ClearCurrentIdentity() => _current.Value = Identity.System;

    /// <summary>
    /// Gets the current identity.
    /// </summary>
    /// <returns>The <see cref="Identity"/>.</returns>
    protected virtual Identity GetCurrent() => _current.Value ?? Identity.System;
}
