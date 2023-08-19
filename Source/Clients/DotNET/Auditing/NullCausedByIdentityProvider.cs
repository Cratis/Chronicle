// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Auditing;

/// <summary>
/// Represents a null implementation of <see cref="ICausedByIdentityProvider"/>.
/// </summary>
public class NullCausedByIdentityProvider : ICausedByIdentityProvider
{
    /// <inheritdoc/>
    public CausedBy GetCurrent() => CausedBy.NotSet;
}
