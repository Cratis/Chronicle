// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// Identifies an erasure and, once recorded, its immutable deletion boundary.
/// </summary>
/// <param name="RequestId">The caller's retry identity.</param>
/// <param name="Through">The captured revision boundary; assigned by the store on first application.</param>
/// <param name="Lifecycle">The lifecycle captured by the store on first application.</param>
/// <param name="Fingerprints">The material covered by this request.</param>
/// <param name="Confirmed">Whether the bounded sweep has been verified and the release grace has elapsed.</param>
public record EncryptionKeyErasureRequest(
    string RequestId,
    EncryptionKeyRevision? Through = null,
    uint Lifecycle = 0,
    IReadOnlyList<string>? Fingerprints = null,
    bool Confirmed = false);
