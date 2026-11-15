// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The result of asking the federation authority to issue a certificate. This is also the body of the enrollment and renewal responses.
/// </summary>
/// <param name="Outcome">The outcome.</param>
/// <param name="Certificate">The issued certificate, DER encoded and base64. Empty unless issued.</param>
/// <param name="Authority">The authority certificate, DER encoded and base64. Empty unless issued.</param>
/// <param name="Identity">The identity in the certificate. Empty unless issued.</param>
/// <param name="Serial">The serial number of the certificate. Empty unless issued.</param>
/// <param name="NotAfter">When the certificate expires.</param>
/// <param name="Endpoint">The current public endpoint of the authority, which can move without re-enrolling.</param>
public record FederationIssuance(
    FederationIssuanceOutcome Outcome,
    string Certificate,
    string Authority,
    string Identity,
    string Serial,
    DateTimeOffset NotAfter,
    string Endpoint)
{
    /// <summary>
    /// Gets the result of a refused request.
    /// </summary>
    public static FederationIssuance Rejected { get; } = new(FederationIssuanceOutcome.Rejected, string.Empty, string.Empty, string.Empty, string.Empty, default, string.Empty);

    /// <summary>
    /// Gets the result of a request whose certificate request could not be honored.
    /// </summary>
    public static FederationIssuance InvalidRequest { get; } = new(FederationIssuanceOutcome.InvalidRequest, string.Empty, string.Empty, string.Empty, string.Empty, default, string.Empty);
}
