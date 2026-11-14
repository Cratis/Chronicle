// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The body of an enrollment request sent to the pre-authentication enrollment route.
/// </summary>
/// <param name="EnrollmentId">The enrollment id from the code.</param>
/// <param name="Secret">The token secret from the code.</param>
/// <param name="Csr">The PKCS#10 certificate request, DER encoded and base64. Only its public key is used; the identity comes from the enrollment.</param>
public record FederationRedeemRequest(string EnrollmentId, string Secret, string Csr)
{
    /// <summary>
    /// Gets a description that never contains the secret.
    /// </summary>
    /// <returns>The description.</returns>
    public override string ToString() => $"FederationRedeemRequest {{ EnrollmentId = {EnrollmentId}, Secret = *** }}";
}
