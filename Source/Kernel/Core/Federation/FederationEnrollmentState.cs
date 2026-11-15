// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The folded state of an enrollment. The token secret is never part of it, only its hash.
/// </summary>
/// <param name="Id">The enrollment id.</param>
/// <param name="Subject">The opaque subject the enrollment is for.</param>
/// <param name="Instance">The kernel instance the identity is issued for.</param>
/// <param name="TokenHash">The SHA-256 of the token secret, base64.</param>
/// <param name="CreatedAt">When the enrollment was created.</param>
/// <param name="ExpiresAt">When the enrollment can no longer be redeemed.</param>
/// <param name="CreatedBy">The identity that created the enrollment.</param>
/// <param name="Redeemed">Whether the enrollment has been redeemed. A redeemed enrollment can never be redeemed again.</param>
/// <param name="Revoked">Whether the enrollment has been revoked.</param>
public record FederationEnrollmentState(
    string Id,
    string Subject,
    string Instance,
    string TokenHash,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    string CreatedBy,
    bool Redeemed,
    bool Revoked);
