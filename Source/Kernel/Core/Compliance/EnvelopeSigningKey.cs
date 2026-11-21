// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

namespace Cratis.Chronicle.Compliance;

/// <summary>
/// An ephemeral ECDSA P-256 signing key, certified by a managed RSA key for one revision and address.
/// </summary>
/// <remarks>
/// The key lives in this process only and is never persisted. Signing is synchronized against disposal, so an entry
/// retired by eviction or by subject erasure never signs after it is disposed. It holds no RSA private key.
/// </remarks>
internal sealed class EnvelopeSigningKey : IDisposable
{
    readonly object _lock = new();
    readonly ECDsa _signer;
    readonly long _expires;
    readonly TimeProvider _timeProvider;
    int _remainingSignatures;
    bool _retired;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvelopeSigningKey"/> class.
    /// </summary>
    /// <param name="signer">The signing key. This instance takes ownership.</param>
    /// <param name="certificate">The managed key's certificate over the signing public key.</param>
    /// <param name="signingKey">The signing public key.</param>
    /// <param name="maximumSignatures">The number of signatures after which the key is rotated.</param>
    /// <param name="lifetime">The time after which the key is rotated.</param>
    /// <param name="timeProvider">The time provider.</param>
    internal EnvelopeSigningKey(ECDsa signer, byte[] certificate, byte[] signingKey, int maximumSignatures, TimeSpan lifetime, TimeProvider timeProvider)
    {
        _signer = signer;
        Certificate = certificate;
        SigningKey = signingKey;
        _remainingSignatures = maximumSignatures;
        _timeProvider = timeProvider;
        _expires = timeProvider.GetTimestamp() + (long)(lifetime.TotalSeconds * timeProvider.TimestampFrequency);
    }

    /// <summary>Gets the managed key's certificate over <see cref="SigningKey"/>.</summary>
    internal byte[] Certificate { get; }

    /// <summary>Gets the signing public key.</summary>
    internal byte[] SigningKey { get; }

    /// <summary>Gets or sets when the key was last used, in ticks of the owning cache's clock.</summary>
    internal long LastUsed { get; set; }

    /// <summary>Gets whether the key is retired or due for rotation.</summary>
    internal bool IsUsable
    {
        get
        {
            lock (_lock)
            {
                return !_retired && _remainingSignatures > 0 && _timeProvider.GetTimestamp() < _expires;
            }
        }
    }

    /// <summary>Signs a message unless the key has been retired.</summary>
    /// <param name="message">The message.</param>
    /// <param name="signature">The IEEE P1363 signature.</param>
    /// <returns>True when signed; false when the key was retired and the caller must obtain another.</returns>
    internal bool TrySign(byte[] message, out byte[] signature)
    {
        lock (_lock)
        {
            if (_retired || _remainingSignatures <= 0)
            {
                signature = [];
                return false;
            }

            _remainingSignatures--;
            signature = _signer.SignData(message, HashAlgorithmName.SHA256, EnvelopeSigning.ValueSignatureFormat);
            return true;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_lock)
        {
            if (_retired) return;
            _retired = true;
            _signer.Dispose();
        }
    }
}
