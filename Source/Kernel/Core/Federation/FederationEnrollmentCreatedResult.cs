// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The result of creating an enrollment.
/// </summary>
/// <param name="Id">The enrollment id.</param>
/// <param name="Code">The enrollment code to hand to the customer. It is a one-time secret that is shown only now.</param>
/// <param name="ExpiresAt">When the code can no longer be redeemed.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Federation.FederationEnrollmentCreatedResult")]
public record FederationEnrollmentCreatedResult([Id(0)] string Id, [Id(1)] string Code, [Id(2)] DateTimeOffset ExpiresAt)
{
    /// <summary>
    /// Gets a description that never contains the code.
    /// </summary>
    /// <returns>The description.</returns>
    public override string ToString() => $"FederationEnrollmentCreatedResult {{ Id = {Id}, Code = ***, ExpiresAt = {ExpiresAt} }}";
}
