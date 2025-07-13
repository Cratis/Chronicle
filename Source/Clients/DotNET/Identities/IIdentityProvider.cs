// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Identities;

/// <summary>
/// Defines a system that can provide the current <see cref="Identity"/>.
/// </summary>
public interface IIdentityProvider
{
    /// <summary>
    /// Gets the current identity.
    /// </summary>
    /// <returns>The <see cref="Identity"/>.</returns>
    Identity GetCurrent();

    /// <summary>
    /// Sets the current identity.
    /// </summary>
    /// <param name="identity">Identity to set.</param>
    void SetCurrentIdentity(Identity identity);

    /// <summary>
    /// Clears the current identity.
    /// </summary>
    void ClearCurrentIdentity();
}
