// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// An enrollment on the authority kernel. The token secret is never part of it.
/// </summary>
/// <param name="Id">The enrollment id.</param>
/// <param name="Subject">The opaque subject the enrollment is for.</param>
/// <param name="Instance">The kernel instance the identity is issued for.</param>
/// <param name="CreatedAt">When the enrollment was created.</param>
/// <param name="ExpiresAt">When the enrollment can no longer be redeemed.</param>
/// <param name="CreatedBy">The identity that created the enrollment.</param>
/// <param name="Status">The status: <c language="text">pending</c>, <c language="text">redeemed</c>, <c language="text">expired</c> or <c language="text">revoked</c>.</param>
public record FederationEnrollmentInformation(
    string Id,
    string Subject,
    string Instance,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    string CreatedBy,
    string Status);
