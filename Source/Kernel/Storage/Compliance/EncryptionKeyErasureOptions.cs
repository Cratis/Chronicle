// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>Controls the release grace before erasure is confirmed.</summary>
public class EncryptionKeyErasureOptions
{
    TimeSpan _revocationGrace = TimeSpan.FromSeconds(5);
    TimeSpan _fenceTimeout = TimeSpan.FromSeconds(3);

    /// <summary>Gets or sets the maximum pre-fence release duration, defaulting to five seconds.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The grace is negative.</exception>
    public TimeSpan RevocationGrace
    {
        get => _revocationGrace;
        set => _revocationGrace = value >= TimeSpan.Zero ? value : throw new ArgumentOutOfRangeException(nameof(value));
    }

    /// <summary>Gets or sets the longest a single fence read or write may wait for the key store, defaulting to three seconds.</summary>
    /// <remarks>An unreachable or unreplicated fence then surfaces as an unavailable fence instead of holding the caller.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The timeout is not positive.</exception>
    public TimeSpan FenceTimeout
    {
        get => _fenceTimeout;
        set => _fenceTimeout = value > TimeSpan.Zero ? value : throw new ArgumentOutOfRangeException(nameof(value));
    }
}
