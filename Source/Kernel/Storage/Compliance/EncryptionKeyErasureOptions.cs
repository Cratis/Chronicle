// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>Controls the release grace before erasure is confirmed.</summary>
public class EncryptionKeyErasureOptions
{
    TimeSpan _revocationGrace = TimeSpan.FromSeconds(5);

    /// <summary>Gets or sets the maximum pre-fence release duration, defaulting to five seconds.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The grace is negative.</exception>
    public TimeSpan RevocationGrace
    {
        get => _revocationGrace;
        set => _revocationGrace = value >= TimeSpan.Zero ? value : throw new ArgumentOutOfRangeException(nameof(value));
    }
}
