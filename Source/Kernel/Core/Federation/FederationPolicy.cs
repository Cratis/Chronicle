// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The policy of a hosted federation subject: whether its links are admitted, and what it may do in each direction.
/// </summary>
/// <param name="Subject">The opaque subject.</param>
/// <param name="Status">Whether the policy admits the subject's links.</param>
/// <param name="Entries">What the subject may do, one entry per direction.</param>
/// <param name="ChangedBy">The identity that last acted on the policy.</param>
[ReadModel]
[BelongsTo(WellKnownServices.Federation)]
[Authorize(Policy = WellKnownAuthorizationPolicies.Administrator)]
public record FederationPolicy(
    string Subject,
    FederationPolicyStatus Status,
    IEnumerable<FederationPolicyEntry> Entries,
    string ChangedBy)
{
    /// <summary>
    /// Gets the policies of every subject.
    /// </summary>
    /// <param name="registry">The <see cref="IFederationCertificateRegistry"/> holding the policies.</param>
    /// <returns>The policies.</returns>
    public static async Task<IEnumerable<FederationPolicy>> AllPolicies(IFederationCertificateRegistry registry) =>
        (await registry.GetPolicies()).Select(_ => new FederationPolicy(_.Subject, _.Status, _.Entries, _.ChangedBy));
}
