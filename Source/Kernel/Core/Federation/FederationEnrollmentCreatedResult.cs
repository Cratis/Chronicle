// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The result of creating an enrollment.
/// </summary>
/// <param name="Id">The enrollment id.</param>
/// <param name="Code">The enrollment code to hand to the customer. It is a one-time secret that is shown only now.</param>
/// <param name="ExpiresAt">When the code can no longer be redeemed.</param>
public record FederationEnrollmentCreatedResult(string Id, string Code, DateTimeOffset ExpiresAt)
{
    /// <summary>
    /// Gets a description that never contains the code.
    /// </summary>
    /// <returns>The description.</returns>
    public override string ToString() => $"FederationEnrollmentCreatedResult {{ Id = {Id}, Code = ***, ExpiresAt = {ExpiresAt} }}";
}
