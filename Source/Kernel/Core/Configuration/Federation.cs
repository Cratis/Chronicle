// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the configuration of the dedicated federation listener.
/// </summary>
/// <remarks>
/// Federation lets other Chronicle instances pull public events from this kernel. It is served on its own port with its own
/// certificate, requires a pinned client certificate over TLS 1.3, and exposes nothing but the federation service.
/// It is off by default.
/// </remarks>
public class Federation
{
    /// <summary>
    /// Gets or inits whether the federation listener is enabled. Defaults to false.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// Gets or inits the port of the federation listener. Defaults to 35100.
    /// </summary>
    public int Port { get; init; } = 35100;

    /// <summary>
    /// Gets or inits the path to the PKCS#12 certificate file the federation listener presents.
    /// </summary>
    public string? CertificatePath { get; init; }

    /// <summary>
    /// Gets or inits the path to a file holding the certificate password. The password itself is never configured inline.
    /// </summary>
    public string? CertificatePasswordPath { get; init; }

    /// <summary>
    /// Gets or inits the named certificates available to consumer bindings as client credentials. Bindings refer to a name, never a value.
    /// </summary>
    public IDictionary<string, FederationCertificate> Certificates { get; init; } = new Dictionary<string, FederationCertificate>(StringComparer.Ordinal);

    /// <summary>
    /// Gets or inits consumer bindings that bootstrap runtime state, keyed by binding id. Runtime state wins when both exist.
    /// </summary>
    public IDictionary<string, FederationBindingConfiguration> Bindings { get; init; } = new Dictionary<string, FederationBindingConfiguration>(StringComparer.Ordinal);

    /// <summary>
    /// Gets or inits producer grants that bootstrap runtime state, keyed by link id. Runtime state wins when both exist.
    /// </summary>
    public IDictionary<string, FederationGrantConfiguration> Grants { get; init; } = new Dictionary<string, FederationGrantConfiguration>(StringComparer.Ordinal);

    /// <summary>
    /// Gets or inits the per-link limits.
    /// </summary>
    public FederationLimits Limits { get; init; } = new();
}
