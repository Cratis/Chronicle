// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Identities;

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
}
