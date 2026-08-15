// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable CA1819 // Allow arrays for properties

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Compliance.MongoDB;

/// <summary>
/// Represents the stored version of an <see cref="EncryptionKeyErasure"/> for a specific
/// <see cref="EncryptionKeyIdentifier"/>.
/// </summary>
/// <remarks>
/// Erasures live in their own collection beside the keys rather than as a document in the key collection, so that
/// nothing on the read path can mistake a fence for key material - the reads sort by revision and would otherwise
/// have to learn to skip it, which is exactly the kind of subtlety a crypto-shred should not depend on.
/// </remarks>
/// <param name="Id">The <see cref="EncryptionKeyIdentifier"/> the erasure was recorded for.</param>
/// <param name="ErasedThrough">The highest <see cref="EncryptionKeyRevision"/> the erasure covered.</param>
/// <param name="ErasedKeyFingerprints">Fingerprints of the key material that was destroyed.</param>
/// <param name="NewKeyAllowed">Whether an authorized new lifecycle may mint a fresh key above the fence.</param>
public record EncryptionKeyErasureForIdentifier(
    EncryptionKeyIdentifier Id,
    EncryptionKeyRevision ErasedThrough,
    string[] ErasedKeyFingerprints,
    bool NewKeyAllowed);
