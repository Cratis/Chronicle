// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Defines how this kernel enrolls with a hosted federation authority and keeps its certificate fresh.
/// </summary>
public interface IFederationPeerEnrollment
{
    /// <summary>
    /// Redeem an enrollment code. Idempotent: when this kernel is already enrolled with the same authority and the certificate is still valid, nothing is contacted.
    /// </summary>
    /// <param name="code">The enrollment code handed over by the operator.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The redemption.</returns>
    /// <exception cref="InvalidFederationEnrollment">Thrown when the code is not valid, the authority cannot be trusted or the enrollment was refused.</exception>
    Task<FederationRedemption> Redeem(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Renew the certificate with a new key when it is past half of its lifetime.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True when the certificate was renewed, false when there was nothing to do or the authority refused.</returns>
    Task<bool> RenewIfDue(CancellationToken cancellationToken = default);
}
