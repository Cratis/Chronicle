// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>The database fence, exact external publications and legacy import markers.</summary>
/// <param name="Erasure">The monotonic fence.</param>
/// <param name="Publications">Exact versions visible to readers.</param>
/// <param name="LegacyImports">Legacy high-water marks and imported closures.</param>
public record EncryptionKeyFenceState(EncryptionKeyErasure Erasure, IReadOnlyList<EncryptionKeyPublication> Publications, IReadOnlyList<EncryptionKeyLegacyImport> LegacyImports)
{
    /// <summary>Gets an empty, open guard.</summary>
    public static EncryptionKeyFenceState Empty => new(new(0u, [], true), [], []);

    /// <summary>Whether a version is covered by the current boundary.</summary>
    /// <param name="revision">The version.</param>
    /// <returns>Whether it is covered.</returns>
    public bool Covers(EncryptionKeyPublication revision) => revision.Revision <= Erasure.ErasedThrough.Value ||
        revision.Lifecycle <= Erasure.ErasedLifecycle || Erasure.ErasedKeyFingerprints.Contains(revision.Fingerprint, StringComparer.Ordinal);

    /// <summary>Build a conditional publication; it does not perform the CAS.</summary>
    /// <param name="identifier">The subject.</param>
    /// <param name="revision">The exact version.</param>
    /// <returns>The replacement state.</returns>
    /// <exception cref="EncryptionKeyErased">The lifecycle or material is covered.</exception>
    public EncryptionKeyFenceState Publish(EncryptionKeyIdentifier identifier, EncryptionKeyPublication revision)
    {
        if (!Erasure.NewKeyAllowed || Covers(revision) || revision.Lifecycle != Erasure.Lifecycle) throw new EncryptionKeyErased(identifier, Erasure);
        return this with
        {
            Erasure = Erasure with { Version = checked(Erasure.Version + 1) },
            Publications = Publications.Where(_ => _.Revision != revision.Revision).Append(revision).ToArray()
        };
    }

    /// <summary>Reject a replacement that weakens a fence or rewrites a request boundary.</summary>
    /// <param name="identifier">The subject.</param>
    /// <param name="next">The replacement.</param>
    /// <exception cref="EncryptionKeyFenceChanged">The replacement weakens the observed state.</exception>
    public void EnsureMonotonic(EncryptionKeyIdentifier identifier, EncryptionKeyFenceState next)
    {
        if (!HasNextVersion(next) || !RetainsBoundary(next) || !RetainsRequests(next))
        {
            throw new EncryptionKeyFenceChanged(identifier);
        }
    }

    /// <summary>Serialize the complete state for the database CAS.</summary>
    /// <returns>The JSON state.</returns>
    public string Serialize() => JsonSerializer.Serialize(this);

    /// <summary>Seal the persisted bytes, including publication and import metadata.</summary>
    /// <param name="state">The serialized state.</param>
    /// <returns>The integrity digest.</returns>
    public static string Seal(string state) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(state)));

    /// <summary>Validate and decode a stored state; malformed or downgraded rows fail closed.</summary>
    /// <param name="identifier">The subject.</param>
    /// <param name="version">The stored CAS version.</param>
    /// <param name="state">The stored JSON.</param>
    /// <param name="seal">The stored seal.</param>
    /// <returns>The validated state.</returns>
    /// <exception cref="EncryptionKeyFenceUnavailable">The stored metadata is malformed or unsealed.</exception>
    public static EncryptionKeyFenceState Read(EncryptionKeyIdentifier identifier, ulong version, string state, string seal)
    {
        try
        {
            var decoded = JsonSerializer.Deserialize<EncryptionKeyFenceState>(state);
            if (decoded?.Erasure is null || decoded.Publications is null || decoded.LegacyImports is null ||
                decoded.Erasure.Version != version || !Seal(state).Equals(seal, StringComparison.Ordinal))
            {
                throw new EncryptionKeyFenceUnavailable(identifier);
            }

            return decoded;
        }
        catch (JsonException)
        {
            throw new EncryptionKeyFenceUnavailable(identifier);
        }
    }

    static bool SameBoundary(EncryptionKeyErasureRequest old, EncryptionKeyErasureRequest current) =>
        current.RequestId == old.RequestId && current.Through == old.Through && current.Lifecycle == old.Lifecycle &&
        (!old.Confirmed || current.Confirmed) && !(old.Fingerprints ?? []).Except(current.Fingerprints ?? [], StringComparer.Ordinal).Any();

    bool HasNextVersion(EncryptionKeyFenceState next) => next.Erasure.Version == checked(Erasure.Version + 1);

    bool RetainsBoundary(EncryptionKeyFenceState next) =>
        next.Erasure.Lifecycle >= Erasure.Lifecycle && next.Erasure.ErasedThrough.Value >= Erasure.ErasedThrough.Value &&
        next.Erasure.ErasedLifecycle >= Erasure.ErasedLifecycle &&
        !Erasure.ErasedKeyFingerprints.Except(next.Erasure.ErasedKeyFingerprints, StringComparer.Ordinal).Any();

    bool RetainsRequests(EncryptionKeyFenceState next) =>
        (Erasure.Requests ?? []).All(old => (next.Erasure.Requests ?? []).Any(current => SameBoundary(old, current)));
}
