// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the configuration of hosted federation, where this kernel acts as the certificate authority for the links of many applications.
/// </summary>
/// <remarks>
/// Hosted federation is served on the federation listener, so <see cref="Federation.Enabled"/> must be set as well. It is off by default.
/// </remarks>
public class FederationHostedOptions
{
    /// <summary>
    /// Gets or inits whether this kernel acts as the federation certificate authority and accepts enrollments. Defaults to false.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// Gets or inits the public https endpoint peers reach the federation listener on, which is put in every enrollment code. There is no built-in default.
    /// </summary>
    public string? PublicEndpoint { get; init; }

    /// <summary>
    /// Gets or inits the trust domain of the identities this kernel issues, <c language="text">spiffe://&lt;trust-domain&gt;/customer/&lt;id&gt;/kernel/&lt;instance&gt;</c>. Defaults to chronicle.
    /// </summary>
    public string TrustDomain { get; init; } = "chronicle";

    /// <summary>
    /// Gets or inits the lifetime of the certificates issued to peers. Defaults to 14 days.
    /// </summary>
    public TimeSpan CertificateLifetime { get; init; } = TimeSpan.FromDays(14);

    /// <summary>
    /// Gets or inits how long an enrollment code can be redeemed. Defaults to 7 days.
    /// </summary>
    public TimeSpan EnrollmentLifetime { get; init; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Gets or inits the lifetime of the listener certificate this kernel issues itself when <see cref="Federation.CertificatePath"/> is not set. Defaults to 30 days.
    /// </summary>
    public TimeSpan ListenerCertificateLifetime { get; init; } = TimeSpan.FromDays(30);

    /// <summary>
    /// Gets or inits how many enrollment and renewal requests one source address may make per minute. Defaults to 10.
    /// </summary>
    public int RequestsPerMinutePerSource { get; init; } = 10;

    /// <summary>
    /// Gets or inits the path to a file holding the key-encryption key, 32 random bytes base64 encoded, that wraps the private keys federation holds.
    /// The key is supplied outside the database, as a mounted secret, and never configured inline. Without it, hosted federation and enrollment
    /// refuse to operate unless the storage backend keeps internal secrets outside the event store databases. A Development environment generates
    /// the file when it is missing; production never does.
    /// </summary>
    public string? KeyEncryptionKeyPath { get; init; }

    /// <summary>
    /// Gets or inits the path to the previous key-encryption key, which stays readable while keys wrapped under it still exist. See the federation documentation for rotation.
    /// </summary>
    public string? PreviousKeyEncryptionKeyPath { get; init; }

    /// <summary>
    /// Gets or inits how many enrollment requests the pre-authentication enrollment routes serve at once. Further requests are refused with 503. Defaults to 16.
    /// </summary>
    public int MaxConcurrentEnrollmentRequests { get; init; } = 16;

    /// <summary>
    /// Gets or inits how many renewal requests are served at once. Renewals have a pool of their own, so anonymous requests that fill
    /// <see cref="MaxConcurrentEnrollmentRequests"/> cannot stop enrolled peers from renewing before their certificates expire. Defaults to 16.
    /// </summary>
    public int MaxConcurrentRenewalRequests { get; init; } = 16;
}
