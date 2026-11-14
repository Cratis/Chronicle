// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// An enrollment that was created on the authority kernel, with the code to hand to the customer.
/// </summary>
/// <param name="Id">The enrollment id.</param>
/// <param name="Code">The enrollment code: one string carrying the public endpoint, the fingerprint of the authority root and the one-time token secret. Treat it as a password.</param>
/// <param name="ExpiresAt">When the code can no longer be redeemed.</param>
public record FederationEnrollmentCode(string Id, string Code, DateTimeOffset ExpiresAt)
{
    /// <summary>
    /// Gets a description that never contains the code.
    /// </summary>
    /// <returns>The description.</returns>
    public override string ToString() => $"FederationEnrollmentCode {{ Id = {Id}, Code = ***, ExpiresAt = {ExpiresAt} }}";
}
