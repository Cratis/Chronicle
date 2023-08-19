// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Auditing;

/// <summary>
/// Defines a system that can provide the current <see cref="CausedBy"/>.
/// /// </summary>
public interface ICausedByIdentityProvider
{
    /// <summary>
    /// Gets the current identity.
    /// </summary>
    /// <returns>The <see cref="CausedBy"/>.</returns>
    CausedBy GetCurrent();
}
