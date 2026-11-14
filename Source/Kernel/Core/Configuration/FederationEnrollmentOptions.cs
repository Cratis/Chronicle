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

    /// <summary>
    /// Gets or inits the client ids of applications that are granted only the kernel capability to redeem an enrollment code and read
    /// the enrollment status of this kernel. Empty by default, which grants nothing. Configuration is the only way to grant it: no
    /// application can be granted this, or anything else, at runtime, and creating or revoking enrollments stays administrator-only.
    /// </summary>
    public IEnumerable<string> Applications { get; init; } = [];
}
