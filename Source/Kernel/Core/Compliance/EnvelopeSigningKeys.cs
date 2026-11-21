// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Security.Cryptography;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance;

/// <summary>
/// A bounded, process-local cache of certified ephemeral signing keys, one per managed-key revision and address.
/// </summary>
/// <remarks>
/// A managed RSA signature costs about 0.8 ms, so it certifies an ephemeral key once and the ephemeral key signs each value.
/// Entries are never persisted, hold no RSA private key, are evicted least recently used beyond the capacity, rotate by
/// signature count and age, and are evicted when the subject is erased. A party able to read this process's memory can
/// sign as the cached keys until they are evicted or rotated; erasing the subject evicts them.
/// </remarks>
internal sealed class EnvelopeSigningKeys
{
    const int EvictionBatch = 64;
    readonly ConcurrentDictionary<(string Fingerprint, uint Revision, string Commitment), Lazy<EnvelopeSigningKey>> _keys = new();
    readonly object _evictionLock = new();
    readonly int _capacity;
    readonly int _maximumSignatures;
    readonly TimeSpan _lifetime;
    readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvelopeSigningKeys"/> class.
    /// </summary>
    /// <param name="capacity">The maximum number of cached keys.</param>
    /// <param name="maximumSignatures">The number of signatures after which a key is rotated.</param>
    /// <param name="lifetime">The age after which a key is rotated.</param>
    /// <param name="timeProvider">The time provider.</param>
    internal EnvelopeSigningKeys(int capacity = 1024, int maximumSignatures = 100_000, TimeSpan? lifetime = null, TimeProvider? timeProvider = null)
    {
        _capacity = capacity;
        _maximumSignatures = maximumSignatures;
        _lifetime = lifetime ?? TimeSpan.FromHours(1);
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>Gets the cache every <see cref="Encryption"/> in the process shares, so erasure can evict it.</summary>
    internal static EnvelopeSigningKeys Shared { get; } = new();

    /// <summary>Gets the number of cached keys.</summary>
    internal int Count => _keys.Count;

    /// <summary>Gets a usable signing key for the revision and address, certifying a new one when needed.</summary>
    /// <param name="key">The managed key.</param>
    /// <param name="revision">The revision of the managed key.</param>
    /// <param name="commitment">The address commitment.</param>
    /// <returns>A usable signing key.</returns>
    internal EnvelopeSigningKey Acquire(EncryptionKey key, EncryptionKeyRevision revision, byte[] commitment)
    {
        var cacheKey = (EnvelopeSigning.Fingerprint(key.Public), revision.Value, Convert.ToHexString(commitment));
        while (true)
        {
            var lazy = _keys.GetOrAdd(cacheKey, _ => new Lazy<EnvelopeSigningKey>(() => Certify(key, revision, commitment)));
            EnvelopeSigningKey signingKey;
            try
            {
                signingKey = lazy.Value;
            }
            catch
            {
                _keys.TryRemove(new(cacheKey, lazy));
                throw;
            }

            if (signingKey.IsUsable)
            {
                signingKey.LastUsed = _timeProvider.GetTimestamp();
                EvictBeyondCapacity();
                return signingKey;
            }

            if (_keys.TryRemove(new(cacheKey, lazy))) signingKey.Dispose();
        }
    }

    /// <summary>Evicts and disposes every key bound to an address, as subject erasure requires.</summary>
    /// <param name="commitment">The address commitment.</param>
    internal void Evict(byte[] commitment)
    {
        var hex = Convert.ToHexString(commitment);
        foreach (var entry in _keys.Where(_ => _.Key.Commitment == hex).ToArray())
        {
            Remove(entry);
        }
    }

    EnvelopeSigningKey Certify(EncryptionKey key, EncryptionKeyRevision revision, byte[] commitment)
    {
        var signer = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        try
        {
            var signingKey = signer.ExportSubjectPublicKeyInfo();
            var certificate = EnvelopeSigning.SignCertificate(key, EnvelopeSigning.CertificateMessage(revision, commitment, key.Public, signingKey));
            return new(signer, certificate, signingKey, _maximumSignatures, _lifetime, _timeProvider);
        }
        catch
        {
            signer.Dispose();
            throw;
        }
    }

    void EvictBeyondCapacity()
    {
        if (_keys.Count <= _capacity) return;
        lock (_evictionLock)
        {
            if (_keys.Count <= _capacity) return;
            var oldest = _keys.Where(_ => _.Value.IsValueCreated).OrderBy(_ => _.Value.Value.LastUsed).Take(EvictionBatch).ToArray();
            foreach (var entry in oldest)
            {
                Remove(entry);
            }
        }
    }

    void Remove(KeyValuePair<(string Fingerprint, uint Revision, string Commitment), Lazy<EnvelopeSigningKey>> entry)
    {
        if (_keys.TryRemove(entry) && entry.Value.IsValueCreated)
        {
            entry.Value.Value.Dispose();
        }
    }
}
