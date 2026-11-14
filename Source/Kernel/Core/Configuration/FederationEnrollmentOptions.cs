// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the configuration of this kernel as a peer that enrolls with a hosted federation authority.
/// </summary>
public class FederationEnrollmentOptions
{
    /// <summary>
    /// Gets or inits the path to a file holding an enrollment code to redeem at startup when this kernel is not yet enrolled.
    /// The code is a one-time secret, so it is read from a file and never configured inline.
    /// </summary>
    public string? CodePath { get; init; }

    /// <summary>
    /// Gets or inits how often the kernel checks whether its certificate is due for renewal. Defaults to 1 hour.
    /// </summary>
    public TimeSpan RenewalCheckInterval { get; init; } = TimeSpan.FromHours(1);
}
