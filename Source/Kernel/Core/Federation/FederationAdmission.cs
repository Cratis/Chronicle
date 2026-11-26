// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Represents the outcome of admitting a hosted federation peer by its certificate.
/// </summary>
/// <param name="Refusal">Why the peer was refused, or <see cref="FederationAdmissionRefusal.None"/> when it was admitted.</param>
/// <param name="Subject">The opaque subject, when admitted.</param>
/// <param name="Instance">The kernel instance of the peer, when admitted.</param>
/// <param name="Policy">The policy of the subject, when admitted.</param>
public record FederationAdmission(FederationAdmissionRefusal Refusal, string Subject, string Instance, FederationPolicyState? Policy)
{
    /// <summary>
    /// Gets whether the peer was admitted.
    /// </summary>
    public bool IsAdmitted => Refusal == FederationAdmissionRefusal.None;

    /// <summary>
    /// Create the outcome of a refused peer.
    /// </summary>
    /// <param name="refusal">Why the peer was refused.</param>
    /// <returns>The outcome.</returns>
    public static FederationAdmission Refused(FederationAdmissionRefusal refusal) => new(refusal, string.Empty, string.Empty, null);
}
