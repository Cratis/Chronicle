// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// An enrollment created on this kernel for a subject. The token secret is never part of it.
/// </summary>
/// <param name="Id">The enrollment id.</param>
/// <param name="Subject">The opaque subject the enrollment is for.</param>
/// <param name="Instance">The kernel instance the identity is issued for.</param>
/// <param name="CreatedAt">When the enrollment was created.</param>
/// <param name="ExpiresAt">When the enrollment can no longer be redeemed.</param>
/// <param name="CreatedBy">The identity that created the enrollment.</param>
/// <param name="Status">The status: <c language="text">pending</c>, <c language="text">redeemed</c>, <c language="text">expired</c> or <c language="text">revoked</c>.</param>
[ReadModel]
[BelongsTo(WellKnownServices.Federation)]
[Authorize(Policy = WellKnownAuthorizationPolicies.Administrator)]
public record FederationEnrollment(
    string Id,
    string Subject,
    string Instance,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    string CreatedBy,
    string Status)
{
    /// <summary>
    /// Gets all enrollments.
    /// </summary>
    /// <param name="registry">The <see cref="IFederationCertificateRegistry"/> holding the enrollments.</param>
    /// <param name="timeProvider">The <see cref="TimeProvider"/> to decide expiry with.</param>
    /// <returns>The enrollments.</returns>
    public static async Task<IEnumerable<FederationEnrollment>> AllEnrollments(IFederationCertificateRegistry registry, TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();
        var enrollments = await registry.GetEnrollments();
        return enrollments.Select(_ => new FederationEnrollment(
            _.Id,
            _.Subject,
            _.Instance,
            _.CreatedAt,
            _.ExpiresAt,
            _.CreatedBy,
            StatusOf(_, now)));
    }

    static string StatusOf(FederationEnrollmentState enrollment, DateTimeOffset now)
    {
        if (enrollment.Revoked)
        {
            return "revoked";
        }

        if (enrollment.Redeemed)
        {
            return "redeemed";
        }

        return now >= enrollment.ExpiresAt ? "expired" : "pending";
    }
}
